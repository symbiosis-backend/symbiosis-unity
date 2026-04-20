using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class BattleCharacterButton : MonoBehaviour
    {
        [Header("Character")]
        [SerializeField] private string characterId;

        [Header("Button UI")]
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text priceText;

        [Header("Standard Card Layout")]
        [SerializeField] private bool applyStandardCardLayout = true;
        [SerializeField] private Vector2 cardSize = new Vector2(300f, 450f);
        [SerializeField] private Vector2 namePosition = new Vector2(0f, 188f);
        [SerializeField] private Vector2 nameSize = new Vector2(260f, 46f);
        [SerializeField] private Vector2 iconPosition = new Vector2(0f, 34f);
        [SerializeField] private Vector2 iconSize = new Vector2(245f, 275f);
        [SerializeField] private Vector2 statsPosition = new Vector2(0f, -165f);
        [SerializeField] private Vector2 statsSize = new Vector2(260f, 116f);
        [SerializeField] private bool showStatsOnlyWhenHighlighted = true;
        [SerializeField] private bool useStatsBackdrop = false;
        [SerializeField] private Vector2 statsBackdropPadding = new Vector2(26f, 18f);
        [SerializeField] private Color statsBackdropColor = new Color(0.06f, 0.055f, 0.045f, 0.78f);
        [SerializeField] private Color statsTextColor = new Color(1f, 0.92f, 0.68f, 1f);
        [SerializeField] private Color statsPriceColor = new Color(1f, 0.78f, 0.18f, 1f);

        [Header("State Roots")]
        [SerializeField] private GameObject lockedRoot;
        [SerializeField] private GameObject selectedRoot;
        [SerializeField] private GameObject disabledRoot;

        [Header("Scale")]
        [SerializeField] private RectTransform scaleTarget;
        [SerializeField] private Vector3 normalScale = Vector3.one;
        [SerializeField] private Vector3 highlightedScale = new Vector3(1.12f, 1.12f, 1f);

        [Header("Shared Preview / Stats Window")]
        [SerializeField] private TMP_Text previewNameText;
        [SerializeField] private TMP_Text previewStatsText;
        [SerializeField] private TMP_Text previewPriceText;
        [SerializeField] private Image previewSelectSpriteImage;
        [SerializeField] private Image previewLobbySpriteImage;
        [SerializeField] private Image previewBattleSpriteImage;

        [Header("Auto Find Preview UI")]
        [SerializeField] private bool autoFindPreviewUI = true;
        [SerializeField] private string previewNameTextObjectName = "PreviewNameText";
        [SerializeField] private string previewStatsTextObjectName = "PreviewStatsText";
        [SerializeField] private string previewPriceTextObjectName = "PreviewPriceText";
        [SerializeField] private string previewSelectImageObjectName = "PreviewSelectImage";
        [SerializeField] private string previewLobbyImageObjectName = "PreviewLobbyImage";
        [SerializeField] private string previewBattleImageObjectName = "PreviewBattleImage";

        [Header("Optional")]
        [SerializeField] private bool refreshOnEnable = true;
        [SerializeField] private bool autoBindClick = true;
        [SerializeField] private bool interactableWhenLocked = true;
        [SerializeField] private bool updatePreviewOnEnable = true;
        [SerializeField] private bool updatePreviewWhenHighlighted = true;
        [SerializeField] private bool selectCharacterOnClick = true;
        [SerializeField] private bool autoUseThisCharacterAsPreviewIfNothingSelected = true;
        [SerializeField] private float purchaseErrorMessageSeconds = 2f;
        [SerializeField] private float purchaseErrorFadeSeconds = 0.45f;

        private BattleCharacterCircularCarousel ownerCarousel;
        private bool isHighlighted;
        private bool subscribed;
        private string transientStatusMessage;
        private float transientStatusUntil;
        private static GameObject purchaseToastObject;
        private static TMP_Text purchaseToastText;
        private static CanvasGroup purchaseToastGroup;
        private static Coroutine purchaseToastRoutine;
        private static BattleCharacterButton purchaseToastRunner;
        private static TMP_FontAsset cachedCharacterNameFont;

        public string CharacterId => characterId;
        public RectTransform RectTransform => transform as RectTransform;
        public Button Button => button;

        private void Reset()
        {
            button = GetComponent<Button>();

            if (scaleTarget == null)
                scaleTarget = transform as RectTransform;

            if (iconImage == null)
                iconImage = FindChildImageByName("Icon");

            if (nameText == null)
                nameText = FindChildTMPByName("NameText");

            if (priceText == null)
                priceText = FindChildTMPByName("PriceText");

        }

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (scaleTarget == null)
                scaleTarget = transform as RectTransform;

            TryResolveLocalUI();
            TryResolvePreviewUI();
        }

        private void Start()
        {
            Refresh();

            if (updatePreviewOnEnable)
                PushPreviewIfNeeded();
        }

        private void OnEnable()
        {
            if (button == null)
                button = GetComponent<Button>();

            TryResolveLocalUI();
            TryResolvePreviewUI();
            ApplyStandardCardLayoutIfNeeded();

            if (autoBindClick && button != null)
            {
                button.onClick.RemoveListener(OnClick);
                button.onClick.AddListener(OnClick);
            }

            Subscribe();
            AppSettings.OnLanguageChanged += OnLanguageChanged;

            if (refreshOnEnable)
                Refresh();

            if (updatePreviewOnEnable)
                PushPreviewIfNeeded();
        }

        private void OnDisable()
        {
            if (autoBindClick && button != null)
                button.onClick.RemoveListener(OnClick);

            Unsubscribe();
            AppSettings.OnLanguageChanged -= OnLanguageChanged;
        }

        private void Update()
        {
            if (!string.IsNullOrEmpty(transientStatusMessage) && Time.unscaledTime >= transientStatusUntil)
            {
                transientStatusMessage = string.Empty;
                Refresh();

                if (ownerCarousel == null || ownerCarousel.CenteredButton == this || IsCharacterSelected())
                    UpdatePreviewWindow();
            }
        }

        private void OnValidate()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (scaleTarget == null)
                scaleTarget = transform as RectTransform;
        }

        public void SetOwnerCarousel(BattleCharacterCircularCarousel carousel)
        {
            ownerCarousel = carousel;
            UpdateCardStatsVisibility();
        }

        public void SetCharacterId(string id, bool refresh = true)
        {
            characterId = id;

            if (refresh)
            {
                Refresh();
                PushPreviewIfNeeded();
            }
        }

        public void SetHighlighted(bool highlighted)
        {
            isHighlighted = highlighted;

            if (scaleTarget != null)
                scaleTarget.localScale = highlighted ? highlightedScale : normalScale;

            UpdateCardStatsVisibility();

            if (highlighted && updatePreviewWhenHighlighted)
                UpdatePreviewWindow();
        }

        public void Refresh()
        {
            BattleCharacterDatabase.BattleCharacterData data = GetCharacterData();
            if (data == null)
            {
                ApplyMissingState();
                return;
            }

            bool unlocked = IsCharacterUnlocked(data);
            bool selected = IsCharacterSelected();

            if (iconImage != null)
            {
                Sprite buttonSprite = data.SelectSprite != null
                    ? data.SelectSprite
                    : data.LobbySprite != null
                        ? data.LobbySprite
                        : data.BattleSprite;

                iconImage.sprite = buttonSprite;
                iconImage.enabled = buttonSprite != null;
                SetImageAlpha(iconImage, buttonSprite != null ? 1f : 0f);
            }

            if (nameText != null)
            {
                ApplyCharacterNameFont(nameText);
                nameText.text = data.LocalizedDisplayName;
            }

            if (priceText != null)
            {
                ApplyStatsFont(priceText);
                priceText.color = statsTextColor;
                priceText.text = BuildCardStatsText(data, unlocked);
            }

            UpdateCardStatsVisibility();

            if (lockedRoot != null)
                lockedRoot.SetActive(!unlocked);

            if (selectedRoot != null)
                selectedRoot.SetActive(selected);

            if (disabledRoot != null)
                disabledRoot.SetActive(!data.IsEnabled);

            BringCardInfoToFront(priceText);
            BringCardInfoToFront(nameText);

            if (button != null)
            {
                bool interactable = data.IsEnabled && (unlocked || interactableWhenLocked);
                button.interactable = interactable;
            }

            if (selected || (autoUseThisCharacterAsPreviewIfNothingSelected && !HasSelectedCharacter()))
                UpdatePreviewWindow();

            if (scaleTarget != null)
                scaleTarget.localScale = isHighlighted ? highlightedScale : normalScale;
        }

        public void OnClick()
        {
            if (ownerCarousel != null)
            {
                ownerCarousel.FocusButton(this, selectCharacterOnClick);
            }
            else if (selectCharacterOnClick)
            {
                SelectDirectly();
            }
            else
            {
                UpdatePreviewWindow();
            }
        }

        public bool SelectDirectly()
        {
            BattleCharacterDatabase.BattleCharacterData data = GetCharacterData();
            if (data == null || !data.IsEnabled)
                return false;

            if (!BattleCharacterSelectionService.HasInstance)
            {
                Debug.LogWarning($"[BattleCharacterButton] Cannot select character before BattleCharacterSelectionService is ready: {characterId}", this);
                UpdatePreviewWindow();
                RefreshAllButtonsInScene();
                return false;
            }

            bool unlocked = BattleCharacterSelectionService.Instance.IsUnlocked(characterId);
            if (!unlocked)
            {
                int price = BattleCharacterSelectionService.Instance.GetUnlockPrice(characterId);
                if (price > 0 && !BattleCharacterSelectionService.Instance.CanAffordCharacter(characterId))
                {
                    ShowPurchaseError(price);
                    RefreshAllButtonsInScene();
                    return false;
                }

                bool purchased = BattleCharacterSelectionService.Instance.SelectOrPurchaseCharacter(characterId, true);
                if (!purchased)
                {
                    ShowPurchaseError(price);
                    Debug.Log($"[BattleCharacterButton] Character locked or not enough currency: {characterId}");
                    UpdatePreviewWindow();
                    RefreshAllButtonsInScene();
                    return false;
                }

                RefreshAllButtonsInScene();
                UpdatePreviewWindow();
                return true;
            }

            bool selected = BattleCharacterSelectionService.Instance.SelectCharacter(characterId, true, true);
            if (!selected)
                return false;

            RefreshAllButtonsInScene();
            UpdatePreviewWindow();
            return true;
        }

        public void UpdatePreviewWindow()
        {
            TryResolvePreviewUI();

            BattleCharacterDatabase.BattleCharacterData data = GetCharacterData();
            if (data == null)
                return;

            bool unlocked = IsCharacterUnlocked(data);

            if (previewNameText != null)
                previewNameText.text = data.LocalizedDisplayName;

            if (previewStatsText != null)
            {
                ApplyStatsFont(previewStatsText);
                previewStatsText.color = statsTextColor;
                previewStatsText.text =
                    $"HP: {data.Stats.MaxHp}\n" +
                    $"ATK: {data.Stats.Attack}\n" +
                    $"ARMOR: {Mathf.RoundToInt(data.Stats.Armor * 100f)}%\n" +
                    $"PARRY: {Mathf.RoundToInt(data.Stats.ParryChance * 100f)}%\n" +
                    $"CRIT: {Mathf.RoundToInt(data.Stats.CritChance * 100f)}%\n" +
                    $"CRIT DMG: x{data.Stats.CritDamageMultiplier:0.##}";
            }

            if (previewPriceText != null)
            {
                ApplyStatsFont(previewPriceText);
                previewPriceText.color = statsTextColor;
                previewPriceText.text = BuildPreviewStatusText(data, unlocked);
            }

            ApplyImage(previewSelectSpriteImage, data.SelectSprite);
            ApplyImage(previewLobbySpriteImage, data.LobbySprite);
            ApplyImage(previewBattleSpriteImage, data.BattleSprite);
        }

        private void TryResolveLocalUI()
        {
            RectTransform root = transform as RectTransform;
            if (scaleTarget == null || scaleTarget == iconImage?.rectTransform)
                scaleTarget = root;

            if (iconImage == null || !IsOwnedByThisCard(iconImage))
                iconImage = FindChildImageByName("Icon");

            if (scaleTarget == null || scaleTarget == iconImage?.rectTransform)
                scaleTarget = root;

            if (nameText == null || !IsOwnedByThisCard(nameText))
                nameText = FindChildTMPByName("NameText");

            if (priceText == null || !IsOwnedByThisCard(priceText))
                priceText = FindChildTMPByName("PriceText") ?? FindStatsPanelTMP() ?? FindFirstStatsTMP();

        }

        private bool IsOwnedByThisCard(Component component)
        {
            return component != null && component.transform != null && component.transform.IsChildOf(transform);
        }

        private void TryResolvePreviewUI()
        {
            if (!autoFindPreviewUI)
                return;

            if (previewNameText == null)
                previewNameText = FindSceneTMPByName(previewNameTextObjectName);

            if (previewStatsText == null)
                previewStatsText = FindSceneTMPByName(previewStatsTextObjectName);

            if (previewPriceText == null)
                previewPriceText = FindSceneTMPByName(previewPriceTextObjectName);

            if (previewSelectSpriteImage == null)
                previewSelectSpriteImage = FindSceneImageByName(previewSelectImageObjectName);

            if (previewLobbySpriteImage == null)
                previewLobbySpriteImage = FindSceneImageByName(previewLobbyImageObjectName);

            if (previewBattleSpriteImage == null)
                previewBattleSpriteImage = FindSceneImageByName(previewBattleImageObjectName);
        }

        [ContextMenu("Battle Character/Apply Standard Card Layout")]
        private void ApplyStandardCardLayoutFromContext()
        {
            TryResolveLocalUI();
            ApplyStandardCardLayoutIfNeeded();
        }

        private void ApplyStandardCardLayoutIfNeeded()
        {
            if (!applyStandardCardLayout)
                return;

            RectTransform root = transform as RectTransform;
            if (root != null)
                ApplyCenteredRect(root, Vector2.zero, cardSize);

            if (iconImage != null)
            {
                ApplyCenteredRect(iconImage.rectTransform, iconPosition, iconSize);
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }

            ApplyTextStyle(
                nameText,
                namePosition,
                nameSize,
                30f,
                18f,
                34f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Color(1f, 0.82f, 0.28f, 1f));
            ApplyCharacterNameFont(nameText);

            ApplyTextStyle(
                priceText,
                statsPosition,
                statsSize,
                21f,
                13f,
                24f,
                FontStyles.Normal,
                TextAlignmentOptions.Center,
                statsTextColor);
            ApplyStatsFont(priceText);

            BringCardInfoToFront(priceText);
            BringCardInfoToFront(nameText);
        }

        private static void ApplyCenteredRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void ApplyTextStyle(
            TMP_Text text,
            Vector2 position,
            Vector2 size,
            float fontSize,
            float minSize,
            float maxSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            Color color)
        {
            if (text == null)
                return;

            ApplyCenteredRect(text.rectTransform, position, size);
            text.alignment = alignment;
            text.fontStyle = fontStyle;
            text.enableAutoSizing = true;
            text.fontSize = fontSize;
            text.fontSizeMin = minSize;
            text.fontSizeMax = maxSize;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.richText = true;
            text.color = color;
            text.raycastTarget = false;
        }

        private void BringCardInfoToFront(Component component)
        {
            if (component == null)
                return;

            Transform root = transform;
            Transform directChild = component.transform;
            while (directChild.parent != null && directChild.parent != root)
                directChild = directChild.parent;

            if (directChild.parent == root)
                directChild.SetAsLastSibling();
        }

        private static void ApplyCharacterNameFont(TMP_Text text)
        {
            if (text == null)
                return;

            TMP_FontAsset font = ResolveCharacterNameFont();
            if (font != null)
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
            }

            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.color = new Color(1f, 0.82f, 0.28f, 1f);
            text.fontSize = 30f;
            text.fontSizeMin = 16f;
            text.fontSizeMax = 32f;
            text.margin = Vector4.zero;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }

        private static void ApplyStatsFont(TMP_Text text)
        {
            if (text == null)
                return;

            TMP_FontAsset font = ResolveCharacterNameFont();
            if (font != null)
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
            }

            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSize = 21f;
            text.fontSizeMin = 13f;
            text.fontSizeMax = 24f;
            text.margin = Vector4.zero;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.alignment = TextAlignmentOptions.Center;
            text.richText = true;
            text.raycastTarget = false;
        }

        private static TMP_FontAsset ResolveCharacterNameFont()
        {
            if (cachedCharacterNameFont != null)
                return cachedCharacterNameFont;

            TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            for (int i = 0; i < fonts.Length; i++)
            {
                TMP_FontAsset font = fonts[i];
                if (font != null && string.Equals(font.name, "Cinzel-VariableFont_wght SDF", StringComparison.OrdinalIgnoreCase))
                {
                    cachedCharacterNameFont = font;
                    return cachedCharacterNameFont;
                }
            }

            return null;
        }

        private BattleCharacterDatabase.BattleCharacterData GetCharacterData()
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return null;

            BattleCharacterDatabase database = BattleCharacterDatabase.HasInstance
                ? BattleCharacterDatabase.Instance
                : FindAnyObjectByType<BattleCharacterDatabase>(FindObjectsInactive.Include);

            if (database == null)
                return null;

            database.RebuildCache();
            return database.GetCharacterOrNull(characterId);
        }

        private bool IsCharacterUnlocked(BattleCharacterDatabase.BattleCharacterData data)
        {
            if (data == null || !data.IsEnabled)
                return false;

            if (BattleCharacterSelectionService.HasInstance)
                return BattleCharacterSelectionService.Instance.IsUnlocked(characterId);

            return data.IsStarterFree || data.UnlockType == BattleCharacterDatabase.CharacterUnlockType.Default;
        }

        private bool IsCharacterSelected()
        {
            if (!BattleCharacterSelectionService.HasInstance)
                return false;

            return string.Equals(
                BattleCharacterSelectionService.Instance.SelectedCharacterId,
                characterId,
                StringComparison.Ordinal);
        }

        private bool HasSelectedCharacter()
        {
            if (!BattleCharacterSelectionService.HasInstance)
                return false;

            return BattleCharacterSelectionService.Instance.HasSelectedCharacter();
        }

        private void PushPreviewIfNeeded()
        {
            if (IsCharacterSelected())
            {
                UpdatePreviewWindow();
                return;
            }

            if (autoUseThisCharacterAsPreviewIfNothingSelected && !HasSelectedCharacter())
                UpdatePreviewWindow();
        }

        private void ApplyImage(Image target, Sprite sprite)
        {
            if (target == null)
                return;

            target.sprite = sprite;
            target.enabled = sprite != null;
            SetImageAlpha(target, sprite != null ? 1f : 0f);
        }

        private void SetImageAlpha(Image image, float alpha)
        {
            if (image == null)
                return;

            Color c = image.color;
            c.a = alpha;
            image.color = c;
        }

        private void Subscribe()
        {
            if (subscribed || !BattleCharacterSelectionService.HasInstance)
                return;

            BattleCharacterSelectionService.Instance.SelectedCharacterChanged += OnSelectedCharacterChanged;
            BattleCharacterSelectionService.Instance.SelectionStateChanged += OnSelectionStateChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;

            if (BattleCharacterSelectionService.HasInstance)
            {
                BattleCharacterSelectionService.Instance.SelectedCharacterChanged -= OnSelectedCharacterChanged;
                BattleCharacterSelectionService.Instance.SelectionStateChanged -= OnSelectionStateChanged;
            }

            subscribed = false;
        }

        private void OnSelectedCharacterChanged(string _)
        {
            Refresh();
        }

        private void OnSelectionStateChanged()
        {
            Refresh();
        }

        private void OnLanguageChanged(GameLanguage language)
        {
            Refresh();

            if (ownerCarousel == null || ownerCarousel.CenteredButton == this || IsCharacterSelected())
                UpdatePreviewWindow();
        }

        private void ApplyMissingState()
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
                SetImageAlpha(iconImage, 0f);
            }

            if (nameText != null)
                nameText.text = "N/A";

            if (priceText != null)
                priceText.text = string.Empty;

            UpdateCardStatsVisibility();

            if (lockedRoot != null)
                lockedRoot.SetActive(false);

            if (selectedRoot != null)
                selectedRoot.SetActive(false);

            if (disabledRoot != null)
                disabledRoot.SetActive(true);

            if (button != null)
                button.interactable = false;

            if (scaleTarget != null)
                scaleTarget.localScale = normalScale;
        }

        private string BuildPriceText(BattleCharacterDatabase.BattleCharacterData data, bool unlocked)
        {
            if (data == null)
                return string.Empty;

            if (!data.IsEnabled)
                return "Disabled";

            if (unlocked)
                return T("battle.character.unlocked", "Unlocked");

            if (BattleCharacterSelectionService.HasInstance)
            {
                int dynamicPrice = BattleCharacterSelectionService.Instance.GetUnlockPrice(data.Id);
                if (dynamicPrice > 0)
                    return FormatPriceRich(dynamicPrice);

                return T("battle.character.free", "Free");
            }

            if (data.IsStarterFree || data.UnlockType == BattleCharacterDatabase.CharacterUnlockType.Default)
                return T("battle.character.free", "Free");

            string currencyName = string.Empty;

            switch (data.PriceCurrency)
            {
                case BattleCharacterDatabase.CharacterPriceCurrencyType.OzAltin:
                    currencyName = GetGoldName();
                    break;

                case BattleCharacterDatabase.CharacterPriceCurrencyType.OzAmetist:
                    currencyName = "Oz Ametist";
                    break;
            }

            if (string.IsNullOrEmpty(currencyName) || data.PriceAmount <= 0)
                return "Locked";

            return $"<b><color=#{ColorUtility.ToHtmlStringRGB(statsPriceColor)}>{FormatPrice(data.PriceAmount)} {currencyName}</color></b>";
        }

        private void UpdateCardStatsVisibility()
        {
            bool visible = !showStatsOnlyWhenHighlighted || ownerCarousel == null || isHighlighted;

            if (priceText != null && priceText.gameObject.activeSelf != visible)
                priceText.gameObject.SetActive(visible);

        }

        private string BuildCardStatsText(BattleCharacterDatabase.BattleCharacterData data, bool unlocked)
        {
            if (data == null)
                return string.Empty;

            string status = BuildCardActionText(data, unlocked);
            string armor = $"{Mathf.RoundToInt(data.Stats.Armor * 100f)}%";
            string crit = $"{Mathf.RoundToInt(data.Stats.CritChance * 100f)}%";

            if (!useStatsBackdrop)
                return
                    $"HP {data.Stats.MaxHp}   ATK {data.Stats.Attack}\n" +
                    $"ARM {armor}   CRIT {crit}\n" +
                    status;

            string markColor = ColorUtility.ToHtmlStringRGBA(statsBackdropColor);
            return
                $"<mark=#{markColor}> HP {data.Stats.MaxHp}   ATK {data.Stats.Attack} </mark>\n" +
                $"<mark=#{markColor}> ARM {armor}   CRIT {crit} </mark>\n" +
                status;
        }

        private string BuildCardActionText(BattleCharacterDatabase.BattleCharacterData data, bool unlocked)
        {
            if (!string.IsNullOrEmpty(transientStatusMessage))
                return transientStatusMessage;

            if (data == null)
                return string.Empty;

            if (!data.IsEnabled)
                return "Disabled";

            if (unlocked)
                return IsCharacterSelected()
                    ? T("battle.character.selected", "Selected")
                    : T("battle.character.select", "Select");

            int price = BattleCharacterSelectionService.HasInstance
                ? BattleCharacterSelectionService.Instance.GetUnlockPrice(data.Id)
                : data.PriceAmount;

            if (price <= 0)
                return $"<b><color=#{ColorUtility.ToHtmlStringRGB(statsPriceColor)}>{T("battle.character.free", "Free")}</color></b>";

            string priceColor = ColorUtility.ToHtmlStringRGB(statsPriceColor);
            return $"{T("battle.character.buy", "Buy")}\n<b><color=#{priceColor}>{FormatPrice(price)} {GetGoldName()}</color></b>";
        }

        private string BuildPreviewStatusText(BattleCharacterDatabase.BattleCharacterData data, bool unlocked)
        {
            if (!string.IsNullOrEmpty(transientStatusMessage))
                return transientStatusMessage;

            return BuildPriceText(data, unlocked);
        }

        private void ShowPurchaseError(int price)
        {
            string need = price > 0
                ? "\n" + GameLocalization.Format("battle.character.need_gold", FormatPrice(price), GetGoldName())
                : string.Empty;

            transientStatusMessage = T("battle.character.not_enough_gold", "Not enough Oz Altin") + need;
            transientStatusUntil = Time.unscaledTime + Mathf.Max(0.5f, purchaseErrorMessageSeconds);
            ShowPurchaseToast(transientStatusMessage);
            UpdatePreviewWindow();
            Refresh();
        }

        private void ShowPurchaseToast(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            EnsurePurchaseToast();
            if (purchaseToastObject == null || purchaseToastText == null || purchaseToastGroup == null)
                return;

            purchaseToastText.text = message;
            purchaseToastGroup.alpha = 1f;
            purchaseToastObject.SetActive(true);

            if (purchaseToastRunner != null && purchaseToastRoutine != null)
                purchaseToastRunner.StopCoroutine(purchaseToastRoutine);

            purchaseToastRunner = this;
            purchaseToastRoutine = StartCoroutine(HidePurchaseToastAfterDelay(
                Mathf.Max(0.5f, purchaseErrorMessageSeconds),
                Mathf.Max(0.05f, purchaseErrorFadeSeconds)));
        }

        private IEnumerator HidePurchaseToastAfterDelay(float delay, float fadeSeconds)
        {
            yield return new WaitForSecondsRealtime(delay);

            float elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;

                if (purchaseToastGroup != null)
                    purchaseToastGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / fadeSeconds));

                yield return null;
            }

            if (purchaseToastGroup != null)
                purchaseToastGroup.alpha = 0f;

            if (purchaseToastObject != null)
                purchaseToastObject.SetActive(false);

            purchaseToastRoutine = null;
            purchaseToastRunner = null;
        }

        private void EnsurePurchaseToast()
        {
            if (purchaseToastObject != null)
                return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Exclude);

            if (canvas == null)
                return;

            purchaseToastObject = new GameObject("BattleCharacterPurchaseToast", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            purchaseToastObject.transform.SetParent(canvas.transform, false);

            RectTransform root = purchaseToastObject.transform as RectTransform;
            if (root != null)
            {
                root.anchorMin = new Vector2(0.5f, 0f);
                root.anchorMax = new Vector2(0.5f, 0f);
                root.pivot = new Vector2(0.5f, 0f);
                root.anchoredPosition = new Vector2(0f, 96f);
                root.sizeDelta = new Vector2(620f, 92f);
            }

            Image background = purchaseToastObject.GetComponent<Image>();
            background.color = new Color(0.08f, 0.07f, 0.06f, 0.92f);
            background.raycastTarget = false;

            purchaseToastGroup = purchaseToastObject.GetComponent<CanvasGroup>();
            purchaseToastGroup.blocksRaycasts = false;
            purchaseToastGroup.interactable = false;

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(purchaseToastObject.transform, false);

            RectTransform textRect = textObject.transform as RectTransform;
            if (textRect != null)
            {
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(24f, 10f);
                textRect.offsetMax = new Vector2(-24f, -10f);
            }

            purchaseToastText = textObject.GetComponent<TMP_Text>();
            purchaseToastText.alignment = TextAlignmentOptions.Center;
            purchaseToastText.enableAutoSizing = true;
            purchaseToastText.fontSize = 24f;
            purchaseToastText.fontSizeMin = 16f;
            purchaseToastText.fontSizeMax = 28f;
            purchaseToastText.textWrappingMode = TextWrappingModes.Normal;
            purchaseToastText.color = new Color(1f, 0.86f, 0.38f, 1f);
            purchaseToastText.raycastTarget = false;
            ApplyCharacterNameFont(purchaseToastText);

            purchaseToastObject.SetActive(false);
        }

        private static string FormatPrice(int price)
        {
            return Mathf.Max(0, price).ToString("#,0", CultureInfo.InvariantCulture);
        }

        private string FormatPriceRich(int price)
        {
            string priceColor = ColorUtility.ToHtmlStringRGB(statsPriceColor);
            return $"<b><color=#{priceColor}>{FormatPrice(price)} {GetGoldName()}</color></b>";
        }

        private static string GetGoldName()
        {
            string value = GameLocalization.Text("common.oz_altin");
            return string.IsNullOrWhiteSpace(value) || string.Equals(value, "common.oz_altin", StringComparison.Ordinal)
                ? "Oz Altin"
                : value;
        }

        private static string T(string key, string fallback)
        {
            string value = GameLocalization.Text(key);
            return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.Ordinal)
                ? fallback
                : value;
        }

        private void RefreshAllButtonsInScene()
        {
            BattleCharacterButton[] allButtons = FindObjectsByType<BattleCharacterButton>(FindObjectsInactive.Exclude);
            for (int i = 0; i < allButtons.Length; i++)
            {
                if (allButtons[i] != null)
                    allButtons[i].Refresh();
            }
        }

        private TMP_Text FindChildTMPByName(string objectName)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && string.Equals(texts[i].name, objectName, StringComparison.OrdinalIgnoreCase))
                    return texts[i];
            }

            return null;
        }

        private TMP_Text FindStatsPanelTMP()
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null || !string.Equals(child.name, "StatsPanel", StringComparison.OrdinalIgnoreCase))
                    continue;

                return child.GetComponentInChildren<TMP_Text>(true);
            }

            return null;
        }

        private TMP_Text FindFirstStatsTMP()
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || text == nameText)
                    continue;

                if (string.Equals(text.name, "NameText", StringComparison.OrdinalIgnoreCase))
                    continue;

                return text;
            }

            return null;
        }

        private Image FindChildImageByName(string objectName)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && string.Equals(images[i].name, objectName, StringComparison.OrdinalIgnoreCase))
                    return images[i];
            }

            return null;
        }

        private static TMP_Text FindSceneTMPByName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text item = texts[i];
                if (item == null || item.gameObject.scene.rootCount == 0)
                    continue;

                if (string.Equals(item.name, objectName, StringComparison.OrdinalIgnoreCase))
                    return item;
            }

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text item = texts[i];
                if (item == null || item.gameObject.scene.rootCount == 0)
                    continue;

                if (item.name.IndexOf(objectName, StringComparison.OrdinalIgnoreCase) >= 0)
                    return item;
            }

            return null;
        }

        private static Image FindSceneImageByName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            Image[] images = Resources.FindObjectsOfTypeAll<Image>();
            for (int i = 0; i < images.Length; i++)
            {
                Image item = images[i];
                if (item == null || item.gameObject.scene.rootCount == 0)
                    continue;

                if (string.Equals(item.name, objectName, StringComparison.OrdinalIgnoreCase))
                    return item;
            }

            for (int i = 0; i < images.Length; i++)
            {
                Image item = images[i];
                if (item == null || item.gameObject.scene.rootCount == 0)
                    continue;

                if (item.name.IndexOf(objectName, StringComparison.OrdinalIgnoreCase) >= 0)
                    return item;
            }

            return null;
        }
    }
}
