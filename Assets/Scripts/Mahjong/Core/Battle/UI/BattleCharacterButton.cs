using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MahjongGame
{

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class BattleCharacterButton : MonoBehaviour
{
	[Header("Character")]
	[SerializeField]
	private string characterId;

	[Header("Button UI")]
	[SerializeField]
	private Button button;

	[SerializeField]
	private Image iconImage;

	[SerializeField]
	private BattleCharacterModelView iconModelView;

	[SerializeField]
	private bool showBattleLobbyCharacterAvatars = true;

	[SerializeField]
	private TMP_Text nameText;

	[SerializeField]
	private TMP_Text priceText;

	[Header("Standard Card Layout")]
	[SerializeField]
	private bool applyStandardCardLayout = true;

	[SerializeField]
	private bool applyBattleLobbyReleaseCardStyle = true;

	[SerializeField]
	private bool applyBattleLobbyDossierCardStyle = true;

	[SerializeField]
	private Vector2 cardSize = new Vector2(300f, 450f);

	[SerializeField]
	private Vector2 battleLobbyReleaseCardSize = new Vector2(280f, 244f);

	[SerializeField]
	private Vector2 namePosition = new Vector2(0f, 188f);

	[SerializeField]
	private Vector2 nameSize = new Vector2(260f, 46f);

	[SerializeField]
	private Vector2 iconPosition = new Vector2(0f, 34f);

	[SerializeField]
	private Vector2 iconSize = new Vector2(245f, 275f);

	[SerializeField]
	private Vector2 statsPosition = new Vector2(0f, -165f);

	[SerializeField]
	private Vector2 statsSize = new Vector2(260f, 116f);

	[SerializeField]
	private bool showStatsOnlyWhenHighlighted = true;

	[SerializeField]
	private bool useStatsBackdrop = true;

	[SerializeField]
	private Vector2 statsBackdropPadding = new Vector2(26f, 18f);

	[SerializeField]
	private Color statsBackdropColor = new Color(0.045f, 0.055f, 0.068f, 0.82f);

	[SerializeField]
	private Color statsTextColor = new Color(1f, 0.92f, 0.68f, 1f);

	[SerializeField]
	private Color statsPriceColor = new Color(1f, 0.78f, 0.18f, 1f);

	[Header("State Roots")]
	[SerializeField]
	private GameObject lockedRoot;

	[SerializeField]
	private GameObject selectedRoot;

	[SerializeField]
	private GameObject disabledRoot;

	[Header("Card Background")]
	[SerializeField]
	private bool ensureVisibleCardBackground = true;

	[SerializeField]
	private Color cardBackgroundColor = new Color(0.055f, 0.07f, 0.088f, 0.92f);

	[SerializeField]
	private Color selectedCardBackgroundColor = new Color(0.2f, 0.145f, 0.065f, 0.96f);

	[Header("Scale")]
	[SerializeField]
	private RectTransform scaleTarget;

	[SerializeField]
	private Vector3 normalScale = Vector3.one;

	[SerializeField]
	private Vector3 highlightedScale = new Vector3(1.12f, 1.12f, 1f);

	[Header("Shared Preview / Stats Window")]
	[SerializeField]
	private TMP_Text previewNameText;

	[SerializeField]
	private TMP_Text previewStatsText;

	[SerializeField]
	private TMP_Text previewPriceText;

	[SerializeField]
	private Image previewSelectSpriteImage;

	[SerializeField]
	private Image previewLobbySpriteImage;

	[SerializeField]
	private Image previewBattleSpriteImage;

	[SerializeField]
	private BattleCharacterModelView previewSelectModelView;

	[SerializeField]
	private BattleCharacterModelView previewLobbyModelView;

	[SerializeField]
	private BattleCharacterModelView previewBattleModelView;

	[Header("Auto Find Preview UI")]
	[SerializeField]
	private bool autoFindPreviewUI = true;

	[SerializeField]
	private string previewNameTextObjectName = "PreviewNameText";

	[SerializeField]
	private string previewStatsTextObjectName = "PreviewStatsText";

	[SerializeField]
	private string previewPriceTextObjectName = "PreviewPriceText";

	[SerializeField]
	private string previewSelectImageObjectName = "PreviewSelectImage";

	[SerializeField]
	private string previewLobbyImageObjectName = "PreviewLobbyImage";

	[SerializeField]
	private string previewBattleImageObjectName = "PreviewBattleImage";

	[Header("Optional")]
	[SerializeField]
	private bool refreshOnEnable = true;

	[SerializeField]
	private bool autoBindClick = true;

	[SerializeField]
	private bool interactableWhenLocked = true;

	[SerializeField]
	private bool updatePreviewOnEnable = true;

	[SerializeField]
	private bool updatePreviewWhenHighlighted = true;

	[SerializeField]
	private bool selectCharacterOnClick = true;

	[SerializeField]
	private bool autoUseThisCharacterAsPreviewIfNothingSelected = true;

	[SerializeField]
	private float purchaseErrorMessageSeconds = 2f;

	[SerializeField]
	private float purchaseErrorFadeSeconds = 0.45f;

	private static readonly Vector2 ProBattleCardSize = new Vector2(282f, 246f);

	private static readonly Vector2 ProBattleCardInnerInset = new Vector2(30f, 28f);

	private static readonly Vector2 ProBattleCardNamePosition = new Vector2(0f, 96f);

	private static readonly Vector2 ProBattleCardNameSize = new Vector2(228f, 30f);

	private static readonly Vector2 ProBattleCardIconPosition = new Vector2(0f, 20f);

	private static readonly Vector2 ProBattleCardIconSize = new Vector2(212f, 128f);

	private static readonly Vector2 ProBattleCardStatsPosition = new Vector2(0f, -82f);

	private static readonly Vector2 ProBattleCardStatsSize = new Vector2(238f, 54f);

	private static readonly Vector2 DossierCardSize = new Vector2(1180f, 660f);

	private static readonly Vector2 DossierMinFullscreenSize = new Vector2(940f, 500f);

	private static readonly Vector2 DossierSideCardSize = new Vector2(500f, 520f);

	private static readonly Color DossierFullscreenTint = new Color(0.02f, 0.016f, 0.012f, 0.42f);

	private static readonly Color DossierFrameTint = new Color(1f, 0.96f, 0.86f, 0.98f);

	private static readonly Color DossierSpriteColor = Color.white;

	private static readonly Color DossierInkColor = new Color(1f, 0.86f, 0.55f, 1f);

	private static readonly Color DossierMutedInkColor = new Color(0.93f, 0.68f, 0.34f, 1f);

	private static readonly Color DossierGoldColor = new Color(1f, 0.78f, 0.22f, 1f);

	private const string DossierWindowSpritePath = "Mahjong/Sprites/BattleLobbyParts/WindowForBattleLobby";

	private const string DossierWidePanelSpritePath = "Mahjong/Sprites/BattleLobbyParts/PartWide";

	private const string DossierThinPanelSpritePath = "Mahjong/Sprites/BattleLobbyParts/PartWideAlt";

	private const string DossierInfoPanelSpritePath = "Mahjong/Sprites/BattleLobbyUI/InfoPanel";

	private const string DossierAvatarFrameSpritePath = "Mahjong/Sprites/BattleLobbyUI/AvatarFrameGenerated";

	private const string OzTileIconSpritePath = "Mahjong/Sprites/BattleTiles/OzTile";

	private const string OzAmetistIconSpritePath = "Mahjong/Sprites/Money/OzAmetist";

	private const string OzAltinIconSpritePath = "Mahjong/Sprites/Money/OzAltın";

	private BattleCharacterCircularCarousel ownerCarousel;

	private bool isHighlighted;

	private bool hasCarouselFrameOverride;

	private Vector2 carouselFramePosition;

	private Vector2 carouselFrameSize;

	private bool carouselFrameIsProfileCard;

	private bool subscribed;

	private bool isDestroying;

	private string transientStatusMessage;

	private float transientStatusUntil;

	private static GameObject purchaseToastObject;

	private static TMP_Text purchaseToastText;

	private static CanvasGroup purchaseToastGroup;

	private static Coroutine purchaseToastRoutine;

	private static BattleCharacterButton purchaseToastRunner;

	private static GameObject purchaseConfirmObject;

	private static TMP_Text purchaseConfirmText;

	private static PurchaseConfirmClickArea purchaseConfirmYesButton;

	private static PurchaseConfirmClickArea purchaseConfirmNoButton;

	private static BattleCharacterButton pendingPurchaseButton;

	private static TMP_FontAsset cachedCharacterNameFont;

	private static Sprite cachedDossierWindowSprite;

	private static Sprite cachedDossierWidePanelSprite;

	private static Sprite cachedDossierThinPanelSprite;

	private static Sprite cachedDossierInfoPanelSprite;

	private static Sprite cachedDossierAvatarFrameSprite;

	private static Sprite cachedOzTileIconSprite;

	private static Sprite cachedOzAmetistIconSprite;

	private static Sprite cachedOzAltinIconSprite;

	private Image releaseCardFront;

	private Image releasePortraitGlow;

	private Image dossierFolderBack;

	private Image dossierProfileWindow;

	private Image dossierTab;

	private Image dossierPaper;

	private Image dossierPhotoMat;

	private Image dossierAvatarFrameOverlay;

	private Image dossierStatsRule;

	private Image dossierAccentRule;

	private TMP_Text dossierDescriptionText;

	private TMP_Text dossierPurchasePriceText;

	private Image dossierPurchaseCurrencyIcon;

	public string CharacterId => characterId;

	public RectTransform RectTransform => base.transform as RectTransform;

	public Button Button => button;

	private void Reset()
	{
		button = GetComponent<Button>();
		if (scaleTarget == null)
		{
			scaleTarget = base.transform as RectTransform;
		}
		if (iconImage == null)
		{
			iconImage = FindChildImageByName("Icon");
		}
		if (nameText == null)
		{
			nameText = FindChildTMPByName("NameText");
		}
		if (priceText == null)
		{
			priceText = FindChildTMPByName("PriceText");
		}
	}

	private void Awake()
	{
		if (button == null)
		{
			button = GetComponent<Button>();
		}
		if (scaleTarget == null)
		{
			scaleTarget = base.transform as RectTransform;
		}
		TryResolveLocalUI();
		TryResolvePreviewUI();
		EnsureButtonRaycastTarget();
	}

	private void Start()
	{
		if (!IsInvalidForCallbacks())
		{
			Refresh();
			if (updatePreviewOnEnable)
			{
				PushPreviewIfNeeded();
			}
		}
	}

	private void OnEnable()
	{
		isDestroying = false;
		if (button == null)
		{
			button = GetComponent<Button>();
		}
		TryResolveLocalUI();
		TryResolvePreviewUI();
		ApplyStandardCardLayoutIfNeeded();
		if (autoBindClick && button != null)
		{
			EnsureButtonRaycastTarget();
			button.onClick.RemoveListener(OnClick);
			button.onClick.AddListener(OnClick);
		}
		Subscribe();
		AppSettings.OnLanguageChanged -= OnLanguageChanged;
		AppSettings.OnLanguageChanged += OnLanguageChanged;
		if (refreshOnEnable)
		{
			Refresh();
		}
		if (updatePreviewOnEnable)
		{
			PushPreviewIfNeeded();
		}
	}

	private void OnDisable()
	{
		if (autoBindClick && button != null)
		{
			button.onClick.RemoveListener(OnClick);
		}
		Unsubscribe();
		AppSettings.OnLanguageChanged -= OnLanguageChanged;
	}

	private void OnDestroy()
	{
		isDestroying = true;
		if (autoBindClick && button != null)
		{
			button.onClick.RemoveListener(OnClick);
		}
		Unsubscribe();
		AppSettings.OnLanguageChanged -= OnLanguageChanged;
	}

	private void Update()
	{
		if (IsInvalidForCallbacks())
		{
			return;
		}
		if (!subscribed)
		{
			Subscribe();
		}
		if (!string.IsNullOrEmpty(transientStatusMessage) && Time.unscaledTime >= transientStatusUntil)
		{
			transientStatusMessage = string.Empty;
			Refresh();
			if (ownerCarousel == null || ownerCarousel.CenteredButton == this || IsCharacterSelected())
			{
				UpdatePreviewWindow();
			}
		}
	}

	private void OnValidate()
	{
		if (button == null)
		{
			button = GetComponent<Button>();
		}
		if (scaleTarget == null)
		{
			scaleTarget = base.transform as RectTransform;
		}
	}

	private void EnsureButtonRaycastTarget()
	{
		if (!(button == null) && !(button.targetGraphic != null))
		{
			Image image = GetComponent<Image>();
			if (image == null)
			{
				image = base.gameObject.AddComponent<Image>();
			}
			image.color = (ensureVisibleCardBackground ? cardBackgroundColor : new Color(1f, 1f, 1f, 0f));
			image.raycastTarget = true;
			button.targetGraphic = image;
		}
	}

	public void SetOwnerCarousel(BattleCharacterCircularCarousel carousel)
	{
		if (!IsInvalidForCallbacks())
		{
			ownerCarousel = carousel;
			UpdateCardStatsVisibility();
		}
	}

	public void SetCharacterId(string id, bool refresh = true)
	{
		if (!IsInvalidForCallbacks())
		{
			characterId = id;
			if (refresh)
			{
				Refresh();
				PushPreviewIfNeeded();
			}
		}
	}

	public void SetHighlighted(bool highlighted)
	{
		if (!IsInvalidForCallbacks() && isHighlighted != highlighted)
		{
			isHighlighted = highlighted;
			if (scaleTarget != null)
			{
				scaleTarget.localScale = (highlighted ? highlightedScale : normalScale);
			}
			RefreshCardStatsText();
			BattleCharacterDatabase.BattleCharacterData characterData = GetCharacterData();
			RefreshDossierDescriptionText(characterData, IsCharacterUnlocked(characterData));
			UpdateCardStatsVisibility();
			ApplyReleaseCardSelectionState(IsCharacterSelected());
			ApplyDossierCardLayoutIfNeeded();
			if (highlighted && updatePreviewWhenHighlighted)
			{
				UpdatePreviewWindow();
			}
		}
	}

	public void SetCarouselFrame(Vector2 position, Vector2 size)
	{
		SetCarouselFrame(position, size, profileCard: false);
	}

	public void SetCarouselFrame(Vector2 position, Vector2 size, bool profileCard)
	{
		hasCarouselFrameOverride = true;
		carouselFramePosition = position;
		carouselFrameSize = size;
		carouselFrameIsProfileCard = profileCard;
		ApplyDossierCardLayoutIfNeeded();
	}

	public void Refresh()
	{
		if (IsInvalidForCallbacks())
		{
			return;
		}
		BattleCharacterDatabase.BattleCharacterData characterData = GetCharacterData();
		if (characterData == null)
		{
			ApplyMissingState();
			return;
		}
		bool flag = IsCharacterUnlocked(characterData);
		bool flag2 = IsCharacterSelected();
		ApplyCardBackground(flag2);
		if (iconImage != null)
		{
			if (!showBattleLobbyCharacterAvatars)
			{
				HideAvatar(iconModelView, iconImage);
			}
			else
			{
				Sprite sprite = ((characterData.SelectSprite != null) ? characterData.SelectSprite : ((characterData.LobbySprite != null) ? characterData.LobbySprite : characterData.BattleSprite));
				if (sprite != null)
				{
					ApplyPortraitSprite(iconModelView, iconImage, sprite);
				}
				else
				{
					ApplyModel(iconModelView, iconImage, characterData, BattleCharacterModelView.ModelContext.Profile);
				}
			}
		}
		if (nameText != null)
		{
			ApplyCharacterNameFont(nameText);
			nameText.text = characterData.LocalizedDisplayName;
		}
		if (priceText != null)
		{
			RefreshCardStatsText(characterData, flag);
		}
		RefreshDossierDescriptionText(characterData, flag);
		UpdateCardStatsVisibility();
		if (lockedRoot != null)
		{
			lockedRoot.SetActive(!flag);
		}
		if (selectedRoot != null)
		{
			selectedRoot.SetActive(!applyBattleLobbyReleaseCardStyle && flag2);
		}
		if (disabledRoot != null)
		{
			disabledRoot.SetActive(!characterData.IsEnabled);
		}
		BringCardInfoToFront(priceText);
		BringCardInfoToFront(nameText);
		if (button != null)
		{
			bool interactable = characterData.IsEnabled && (flag || interactableWhenLocked);
			button.interactable = interactable;
		}
		if (flag2 || (autoUseThisCharacterAsPreviewIfNothingSelected && !HasSelectedCharacter()))
		{
			UpdatePreviewWindow();
		}
		if (scaleTarget != null)
		{
			scaleTarget.localScale = (isHighlighted ? highlightedScale : normalScale);
		}
	}

	public void OnClick()
	{
		if (!IsInvalidForCallbacks())
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
	}

	public bool SelectDirectly()
	{
		if (IsInvalidForCallbacks())
		{
			return false;
		}
		BattleCharacterDatabase.BattleCharacterData characterData = GetCharacterData();
		if (characterData == null || !characterData.IsEnabled)
		{
			return false;
		}
		if (!BattleCharacterSelectionService.HasInstance)
		{
			Debug.LogWarning("[BattleCharacterButton] Cannot select character before BattleCharacterSelectionService is ready: " + characterId, this);
			UpdatePreviewWindow();
			RefreshAllButtonsInScene();
			return false;
		}
		if (!BattleCharacterSelectionService.Instance.IsUnlocked(characterId))
		{
			int unlockPrice = BattleCharacterSelectionService.Instance.GetUnlockPrice(characterId);
			if (unlockPrice > 0 && !BattleCharacterSelectionService.Instance.CanAffordCharacter(characterId))
			{
				ShowPurchaseError(unlockPrice);
				RefreshAllButtonsInScene();
				return false;
			}
			if (unlockPrice <= 0)
			{
				if (!BattleCharacterSelectionService.Instance.SelectOrPurchaseCharacter(characterId))
				{
					RefreshAllButtonsInScene();
					return false;
				}
				RefreshAllButtonsInScene();
				UpdatePreviewWindow();
				RefreshBattleLobbySelectedCharacterViews();
				CloseBattleLobbyCarouselAfterSelection();
				return true;
			}
			ShowPurchaseConfirm(characterData, unlockPrice);
			return false;
		}
		if (!BattleCharacterSelectionService.Instance.SelectCharacter(characterId))
		{
			return false;
		}
		RefreshAllButtonsInScene();
		UpdatePreviewWindow();
		RefreshBattleLobbySelectedCharacterViews();
		CloseBattleLobbyCarouselAfterSelection();
		return true;
	}

	private void ConfirmPendingPurchase()
	{
		HidePurchaseConfirm();
		if (IsInvalidForCallbacks())
		{
			return;
		}
		if (!BattleCharacterSelectionService.HasInstance)
		{
			return;
		}
		int unlockPrice = BattleCharacterSelectionService.Instance.GetUnlockPrice(characterId);
		if (!BattleCharacterSelectionService.Instance.SelectOrPurchaseCharacter(characterId))
		{
			ShowPurchaseError(unlockPrice);
			Debug.Log("[BattleCharacterButton] Character locked or not enough currency: " + characterId);
			UpdatePreviewWindow();
			RefreshAllButtonsInScene();
			return;
		}
		RefreshAllButtonsInScene();
		UpdatePreviewWindow();
		RefreshBattleLobbySelectedCharacterViews();
		CloseBattleLobbyCarouselAfterSelection();
	}

	private static void CloseBattleLobbyCarouselAfterSelection()
	{
		BattleLobbyUI battleLobbyUI = UnityEngine.Object.FindAnyObjectByType<BattleLobbyUI>(FindObjectsInactive.Include);
		if (battleLobbyUI != null && battleLobbyUI.isActiveAndEnabled)
		{
			battleLobbyUI.RequestRestoreLobbyHudAfterCharacterCarouselClosed();
		}
	}

	private static void RefreshBattleLobbySelectedCharacterViews()
	{
		BattleLobbyUI battleLobbyUI = UnityEngine.Object.FindAnyObjectByType<BattleLobbyUI>(FindObjectsInactive.Include);
		if (battleLobbyUI != null)
		{
			battleLobbyUI.RefreshSelectedBattleCharacterViews();
		}
	}

	public void UpdatePreviewWindow()
	{
		if (IsInvalidForCallbacks())
		{
			return;
		}
		TryResolvePreviewUI();
		BattleCharacterDatabase.BattleCharacterData characterData = GetCharacterData();
		if (characterData != null)
		{
			bool unlocked = IsCharacterUnlocked(characterData);
			if (previewNameText != null)
			{
				previewNameText.text = characterData.LocalizedDisplayName;
			}
			if (previewStatsText != null)
			{
				ApplyStatsFont(previewStatsText);
				previewStatsText.color = statsTextColor;
				previewStatsText.text = BattleStatIconProvider.ValueWithIconGap(string.Format("{0}: {1}", T("battle.character.stat.hp", "HP"), characterData.Stats.MaxHp)) + "\n" + BattleStatIconProvider.ValueWithIconGap(string.Format("{0}: {1}", T("battle.character.stat.attack", "Attack"), characterData.Stats.Attack)) + "\n" + BattleStatIconProvider.ValueWithIconGap(string.Format("{0}: {1}%", T("battle.character.stat.armor", "Armor"), Mathf.RoundToInt(characterData.Stats.Armor * 100f))) + "\n" + BattleStatIconProvider.ValueWithIconGap(string.Format("{0}: {1}%", T("battle.character.stat.crit", "Crit"), Mathf.RoundToInt(characterData.Stats.CritChance * 100f))) + "\n" + BattleStatIconProvider.ValueWithIconGap(string.Format("{0}: x{1:0.##}", T("battle.character.stat.crit_damage", "Crit Damage"), characterData.Stats.CritDamageMultiplier));
				ApplyPreviewStatsIcons(previewStatsText);
			}
			if (previewPriceText != null)
			{
				ApplyStatsFont(previewPriceText);
				previewPriceText.color = statsTextColor;
				previewPriceText.text = BuildPreviewStatusText(characterData, unlocked);
			}
			if (showBattleLobbyCharacterAvatars)
			{
				ApplyPreviewModelOrImage(ref previewSelectModelView, previewSelectSpriteImage, characterData, BattleCharacterModelView.ModelContext.Profile, characterData.SelectSprite);
				ApplyPreviewModelOrImage(ref previewLobbyModelView, previewLobbySpriteImage, characterData, BattleCharacterModelView.ModelContext.Lobby, characterData.LobbySprite);
				ApplyPreviewModelOrImage(ref previewBattleModelView, previewBattleSpriteImage, characterData, BattleCharacterModelView.ModelContext.Battle, characterData.BattleSprite);
			}
			else
			{
				HideAvatar(previewSelectModelView, previewSelectSpriteImage);
				HideAvatar(previewLobbyModelView, previewLobbySpriteImage);
				HideAvatar(previewBattleModelView, previewBattleSpriteImage);
			}
		}
	}

	private void ApplyPreviewStatsIcons(TMP_Text text)
	{
		RectTransform rectTransform = ((text != null) ? text.rectTransform : null);
		if (!(rectTransform == null))
		{
			Vector2 size = new Vector2(24f, 24f);
			BattleStatIconProvider.ShowIcon(rectTransform, "PreviewHpIcon", BattleStatIconKind.Hp, new Vector2(-118f, 54f), size);
			BattleStatIconProvider.ShowIcon(rectTransform, "PreviewAttackIcon", BattleStatIconKind.Attack, new Vector2(-118f, 27f), size);
			BattleStatIconProvider.ShowIcon(rectTransform, "PreviewArmorIcon", BattleStatIconKind.Armor, new Vector2(-118f, 0f), size);
			BattleStatIconProvider.ShowIcon(rectTransform, "PreviewCriticalIcon", BattleStatIconKind.Critical, new Vector2(-118f, -27f), size);
			BattleStatIconProvider.ShowIcon(rectTransform, "PreviewCriticalPowerIcon", BattleStatIconKind.CriticalPower, new Vector2(-118f, -54f), size);
		}
	}

	private void TryResolveLocalUI()
	{
		RectTransform rectTransform = base.transform as RectTransform;
		if (scaleTarget == null || scaleTarget == iconImage?.rectTransform)
		{
			scaleTarget = rectTransform;
		}
		if (iconImage == null || !IsOwnedByThisCard(iconImage))
		{
			iconImage = FindChildImageByName("Icon");
		}
		if (iconImage != null && (iconModelView == null || !IsOwnedByThisCard(iconModelView)))
		{
			iconModelView = EnsureModelView(iconImage);
		}
		if (scaleTarget == null || scaleTarget == iconImage?.rectTransform)
		{
			scaleTarget = rectTransform;
		}
		if (nameText == null || !IsOwnedByThisCard(nameText))
		{
			nameText = FindChildTMPByName("NameText");
		}
		if (priceText == null || !IsOwnedByThisCard(priceText))
		{
			priceText = FindChildTMPByName("PriceText") ?? FindStatsPanelTMP() ?? FindFirstStatsTMP();
		}
	}

	private bool IsOwnedByThisCard(Component component)
	{
		if (component != null && component.transform != null)
		{
			return component.transform.IsChildOf(base.transform);
		}
		return false;
	}

	private void TryResolvePreviewUI()
	{
		if (autoFindPreviewUI)
		{
			if (previewNameText == null)
			{
				previewNameText = FindSceneTMPByName(previewNameTextObjectName);
			}
			if (previewStatsText == null)
			{
				previewStatsText = FindSceneTMPByName(previewStatsTextObjectName);
			}
			if (previewPriceText == null)
			{
				previewPriceText = FindSceneTMPByName(previewPriceTextObjectName);
			}
			if (previewSelectSpriteImage == null)
			{
				previewSelectSpriteImage = FindSceneImageByName(previewSelectImageObjectName);
			}
			if (previewLobbySpriteImage == null)
			{
				previewLobbySpriteImage = FindSceneImageByName(previewLobbyImageObjectName);
			}
			if (previewBattleSpriteImage == null)
			{
				previewBattleSpriteImage = FindSceneImageByName(previewBattleImageObjectName);
			}
			if (previewSelectSpriteImage != null && previewSelectModelView == null)
			{
				previewSelectModelView = EnsureModelView(previewSelectSpriteImage);
			}
			if (previewLobbySpriteImage != null && previewLobbyModelView == null)
			{
				previewLobbyModelView = EnsureModelView(previewLobbySpriteImage);
			}
			if (previewBattleSpriteImage != null && previewBattleModelView == null)
			{
				previewBattleModelView = EnsureModelView(previewBattleSpriteImage);
			}
		}
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
		{
			return;
		}
		if (applyBattleLobbyDossierCardStyle)
		{
			ApplyDossierCardLayoutIfNeeded();
			return;
		}
		RectTransform rectTransform = base.transform as RectTransform;
		Vector2 size = (applyBattleLobbyReleaseCardStyle ? ProBattleCardSize : cardSize);
		if (rectTransform != null)
		{
			ApplyCenteredRect(rectTransform, Vector2.zero, size);
		}
		if (iconImage != null)
		{
			Vector2 position = (applyBattleLobbyReleaseCardStyle ? ProBattleCardIconPosition : iconPosition);
			Vector2 size2 = (applyBattleLobbyReleaseCardStyle ? ProBattleCardIconSize : iconSize);
			ApplyCenteredRect(iconImage.rectTransform, position, size2);
			iconImage.preserveAspect = true;
			iconImage.raycastTarget = false;
		}
		ApplyTextStyle(nameText, applyBattleLobbyReleaseCardStyle ? ProBattleCardNamePosition : namePosition, applyBattleLobbyReleaseCardStyle ? ProBattleCardNameSize : nameSize, applyBattleLobbyReleaseCardStyle ? 20f : 30f, applyBattleLobbyReleaseCardStyle ? 14f : 18f, applyBattleLobbyReleaseCardStyle ? 24f : 34f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.82f, 0.28f, 1f));
		ApplyCharacterNameFont(nameText);
		ApplyTextStyle(priceText, applyBattleLobbyReleaseCardStyle ? ProBattleCardStatsPosition : statsPosition, applyBattleLobbyReleaseCardStyle ? ProBattleCardStatsSize : statsSize, applyBattleLobbyReleaseCardStyle ? 13f : 21f, applyBattleLobbyReleaseCardStyle ? 10f : 13f, applyBattleLobbyReleaseCardStyle ? 16f : 24f, FontStyles.Normal, TextAlignmentOptions.Center, statsTextColor);
		ApplyStatsFont(priceText);
		BringCardInfoToFront(priceText);
		BringCardInfoToFront(nameText);
		BringCardInfoToFront(iconImage);
	}

	private void ApplyDossierCardLayoutIfNeeded()
	{
		if (!applyBattleLobbyDossierCardStyle)
		{
			return;
		}
		RectTransform rectTransform = base.transform as RectTransform;
		if (rectTransform == null)
		{
			return;
		}
		bool flag = isHighlighted || ownerCarousel == null;
		IsCharacterSelected();
		Vector2 size = hasCarouselFrameOverride ? carouselFrameSize : (flag ? ResolveFullscreenDossierSize(rectTransform) : DossierSideCardSize);
		float num = (flag ? ResolveDossierScaleFactor(size) : 1f);
		ApplyCenteredRect(rectTransform, hasCarouselFrameOverride ? carouselFramePosition : Vector2.zero, size);
		EnsureDossierChrome(rectTransform);
		BattleCharacterDatabase.BattleCharacterData characterData = GetCharacterData();
		if (characterData != null)
		{
			RefreshDossierDescriptionText(characterData, IsCharacterUnlocked(characterData));
		}
		if (hasCarouselFrameOverride && carouselFrameIsProfileCard && !flag)
		{
			ApplyDossierProfileRailCardLayout(rectTransform, size, characterData);
			return;
		}
		if (flag)
		{
			ApplyDossierMainCardLayout(rectTransform, size, num, characterData);
			return;
		}
		if (dossierFolderBack != null)
		{
			ApplyCenteredRect(dossierFolderBack.rectTransform, Vector2.zero, size);
			if (flag)
			{
				dossierFolderBack.enabled = false;
				dossierFolderBack.sprite = null;
				dossierFolderBack.raycastTarget = false;
			}
			else
			{
				ApplyDossierPartSprite(dossierFolderBack, LoadDossierSprite(ref cachedDossierWindowSprite, "Mahjong/Sprites/BattleLobbyParts/WindowForBattleLobby"), DossierSpriteColor);
			}
			dossierFolderBack.transform.SetAsFirstSibling();
		}
		if (dossierProfileWindow != null)
		{
			if (flag)
			{
				ApplyCenteredRect(dossierProfileWindow.rectTransform, ScaleVector(new Vector2(0f, 4f), num), ScaleVector(new Vector2(1080f, 476f), num));
				BattlePopupStyle.ApplyWindow(dossierProfileWindow, raycastTarget: false);
				dossierProfileWindow.gameObject.SetActive(value: true);
				dossierProfileWindow.transform.SetAsFirstSibling();
			}
			else
			{
				dossierProfileWindow.gameObject.SetActive(value: false);
			}
		}
		if (dossierTab != null)
		{
			Vector2 size2 = (flag ? ScaleVector(new Vector2(500f, 54f), num) : new Vector2(360f, 44f));
			Vector2 position = (flag ? ScaleVector(new Vector2(-326f, 214f), num) : new Vector2(0f, 236f));
			ApplyCenteredRect(dossierTab.rectTransform, position, size2);
			ApplyDossierPartSprite(dossierTab, LoadDossierSprite(ref cachedDossierThinPanelSprite, "Mahjong/Sprites/BattleLobbyParts/PartWideAlt"), DossierSpriteColor);
			dossierTab.gameObject.SetActive(value: true);
		}
		if (dossierPaper != null)
		{
			ApplyCenteredRect(dossierPaper.rectTransform, flag ? ScaleVector(new Vector2(228f, -16f), num) : new Vector2(0f, -12f), flag ? ScaleVector(new Vector2(760f, 360f), num) : new Vector2(410f, 395f));
			if (flag)
			{
				ApplyDossierPartSprite(dossierPaper, LoadDossierSprite(ref cachedDossierInfoPanelSprite, "Mahjong/Sprites/BattleLobbyUI/InfoPanel"), DossierSpriteColor);
			}
			else
			{
				ApplyDossierPartSprite(dossierPaper, LoadDossierSprite(ref cachedDossierWidePanelSprite, "Mahjong/Sprites/BattleLobbyParts/PartWide"), DossierSpriteColor);
			}
			dossierPaper.gameObject.SetActive(value: true);
		}
		if (dossierPhotoMat != null)
		{
			ApplyCenteredRect(dossierPhotoMat.rectTransform, flag ? ScaleVector(new Vector2(-392f, -4f), num) : new Vector2(0f, 32f), flag ? ScaleVector(new Vector2(348f, 318f), num) : new Vector2(332f, 295f));
			if (flag)
			{
				ApplyDossierPartSprite(dossierPhotoMat, LoadDossierSprite(ref cachedDossierInfoPanelSprite, "Mahjong/Sprites/BattleLobbyUI/InfoPanel"), DossierSpriteColor);
			}
			else
			{
				ApplyDossierPartSprite(dossierPhotoMat, LoadDossierSprite(ref cachedDossierWidePanelSprite, "Mahjong/Sprites/BattleLobbyParts/PartWide"), DossierSpriteColor);
			}
			dossierPhotoMat.gameObject.SetActive(value: true);
		}
		if (iconImage != null)
		{
			Sprite sprite = ResolveDossierPortraitSprite(characterData);
			ApplyCenteredRect(iconImage.rectTransform, flag ? ScaleVector(new Vector2(-392f, -4f), num) : new Vector2(0f, 34f), flag ? ScaleVector(new Vector2(286f, 274f), num) : new Vector2(286f, 248f));
			if (sprite != null)
			{
				ApplyPortraitSprite(iconModelView, iconImage, sprite);
			}
			else
			{
				HideAvatar(iconModelView, iconImage);
			}
			iconImage.preserveAspect = true;
			iconImage.raycastTarget = false;
		}
		if (dossierAvatarFrameOverlay != null)
		{
			ApplyCenteredRect(dossierAvatarFrameOverlay.rectTransform, flag ? ScaleVector(new Vector2(-392f, -4f), num) : new Vector2(0f, 34f), flag ? ScaleVector(new Vector2(338f, 324f), num) : new Vector2(332f, 295f));
			ApplyDossierPartSprite(dossierAvatarFrameOverlay, LoadDossierSprite(ref cachedDossierAvatarFrameSprite, DossierAvatarFrameSpritePath), DossierSpriteColor);
			dossierAvatarFrameOverlay.gameObject.SetActive(value: true);
		}
		ApplyTextStyle(nameText, flag ? ScaleVector(new Vector2(-326f, 214f), num) : new Vector2(0f, 258f), flag ? ScaleVector(new Vector2(510f, 54f), num) : new Vector2(392f, 48f), flag ? (38f * num) : 34f, flag ? (24f * num) : 22f, flag ? (44f * num) : 40f, FontStyles.Bold, TextAlignmentOptions.Center, DossierGoldColor);
		ApplyCharacterNameFont(nameText);
		if (nameText != null)
		{
			ApplyDossierTextContrast(nameText, DossierGoldColor, 0.18f);
			nameText.fontSize = (flag ? (38f * num) : 34f);
			nameText.fontSizeMin = (flag ? (24f * num) : 22f);
			nameText.fontSizeMax = (flag ? (44f * num) : 40f);
			nameText.margin = new Vector4(6f, 0f, 6f, 0f);
			nameText.overflowMode = TextOverflowModes.Truncate;
		}
		ApplyTextStyle(priceText, flag ? ScaleVector(new Vector2(250f, 80f), num) : new Vector2(0f, -170f), flag ? ScaleVector(new Vector2(570f, 150f), num) : new Vector2(390f, 82f), flag ? (23f * num) : 18f, flag ? (15f * num) : 11f, flag ? (27f * num) : 21f, FontStyles.Bold, flag ? TextAlignmentOptions.Left : TextAlignmentOptions.Center, flag ? DossierInkColor : DossierGoldColor);
		ApplyStatsFont(priceText);
		if (priceText != null)
		{
			ApplyDossierTextContrast(priceText, flag ? DossierInkColor : DossierGoldColor, flag ? 0.16f : 0.14f);
			priceText.fontSize = (flag ? (23f * num) : 18f);
			priceText.fontSizeMin = (flag ? (15f * num) : 11f);
			priceText.fontSizeMax = (flag ? (27f * num) : 21f);
			priceText.alignment = (flag ? TextAlignmentOptions.Left : TextAlignmentOptions.Center);
			priceText.overflowMode = TextOverflowModes.Truncate;
		}
		if (dossierDescriptionText != null)
		{
			ApplyTextStyle(dossierDescriptionText, ScaleVector(new Vector2(250f, -132f), num), ScaleVector(new Vector2(570f, 116f), num), 18f * num, 12f * num, 21f * num, FontStyles.Normal, TextAlignmentOptions.Left, DossierInkColor);
			ApplyDossierTextContrast(dossierDescriptionText, DossierMutedInkColor, 0.14f);
			dossierDescriptionText.gameObject.SetActive(flag);
		}
		if (dossierStatsRule != null)
		{
			ApplyCenteredRect(dossierStatsRule.rectTransform, flag ? ScaleVector(new Vector2(250f, -42f), num) : new Vector2(250f, -72f), flag ? ScaleVector(new Vector2(560f, 24f), num) : new Vector2(410f, 20f));
			ApplyDossierPartSprite(dossierStatsRule, LoadDossierSprite(ref cachedDossierThinPanelSprite, "Mahjong/Sprites/BattleLobbyParts/PartWideAlt"), DossierSpriteColor);
			dossierStatsRule.gameObject.SetActive(flag);
		}
		if (dossierAccentRule != null)
		{
			ApplyCenteredRect(dossierAccentRule.rectTransform, flag ? new Vector2(0f, (0f - size.y) * 0.42f) : new Vector2(0f, -254f), flag ? new Vector2(size.x * 0.78f, 28f) : new Vector2(410f, 20f));
			ApplyDossierPartSprite(dossierAccentRule, LoadDossierSprite(ref cachedDossierThinPanelSprite, "Mahjong/Sprites/BattleLobbyParts/PartWideAlt"), DossierSpriteColor);
			dossierAccentRule.gameObject.SetActive(value: true);
		}
		BringCardInfoToFront(dossierPaper);
		BringCardInfoToFront(dossierPhotoMat);
		BringCardInfoToFront(iconImage);
		BringCardInfoToFront(dossierAvatarFrameOverlay);
		BringCardInfoToFront(nameText);
		BringCardInfoToFront(priceText);
		BringCardInfoToFront(dossierDescriptionText);
		BringCardInfoToFront(dossierStatsRule);
		BringCardInfoToFront(dossierAccentRule);
	}

	private void ApplyDossierMainCardLayout(RectTransform rectTransform, Vector2 size, float scale, BattleCharacterDatabase.BattleCharacterData characterData)
	{
		EnsureDossierChrome(rectTransform);
		Vector2 windowSize = new Vector2(size.x * 0.9f, size.y * 0.7f);
		Vector2 photoCenter = new Vector2((0f - size.x) * 0.27f, windowSize.y * 0.18f);
		Vector2 titleCenter = new Vector2(size.x * 0.2f, windowSize.y * 0.46f);
		Vector2 statsCenter = new Vector2(photoCenter.x, -windowSize.y * 0.53f);
		Vector2 dossierCenter = new Vector2(size.x * 0.18f, windowSize.y * 0.03f);
		if (dossierFolderBack != null)
		{
			dossierFolderBack.gameObject.SetActive(value: false);
		}
		if (dossierProfileWindow != null)
		{
			dossierProfileWindow.gameObject.SetActive(value: false);
		}
		if (dossierTab != null)
		{
			ApplyCenteredRect(dossierTab.rectTransform, titleCenter, new Vector2(760f * scale, 88f * scale));
			ApplyDossierPartSprite(dossierTab, LoadDossierSprite(ref cachedDossierThinPanelSprite, DossierThinPanelSpritePath), DossierSpriteColor);
			dossierTab.gameObject.SetActive(value: true);
		}
		if (dossierPaper != null)
		{
			dossierPaper.gameObject.SetActive(value: false);
		}
		if (dossierPhotoMat != null)
		{
			ApplyCenteredRect(dossierPhotoMat.rectTransform, photoCenter, new Vector2(390f * scale, 390f * scale));
			ApplyDossierPartSprite(dossierPhotoMat, LoadDossierSprite(ref cachedDossierInfoPanelSprite, DossierInfoPanelSpritePath), DossierSpriteColor);
			dossierPhotoMat.gameObject.SetActive(value: true);
		}
		if (iconImage != null)
		{
			Sprite sprite = ResolveDossierPortraitSprite(characterData);
			ApplyCenteredRect(iconImage.rectTransform, photoCenter, new Vector2(326f * scale, 326f * scale));
			if (sprite != null)
			{
				ApplyPortraitSprite(iconModelView, iconImage, sprite);
			}
			else
			{
				HideAvatar(iconModelView, iconImage);
			}
			iconImage.preserveAspect = true;
			iconImage.raycastTarget = false;
		}
		if (dossierAvatarFrameOverlay != null)
		{
			ApplyCenteredRect(dossierAvatarFrameOverlay.rectTransform, photoCenter, new Vector2(424f * scale, 424f * scale));
			ApplyAvatarFrameSprite(dossierAvatarFrameOverlay);
			dossierAvatarFrameOverlay.gameObject.SetActive(value: true);
		}
		ApplyTextStyle(nameText, titleCenter, new Vector2(740f * scale, 92f * scale), 72f * scale, 52f * scale, 82f * scale, FontStyles.Bold, TextAlignmentOptions.Center, DossierGoldColor);
		ApplyCharacterNameFont(nameText);
		if (nameText != null)
		{
			ApplyDossierTextContrast(nameText, DossierGoldColor, 0.28f);
			nameText.fontStyle = FontStyles.Bold;
			nameText.enableAutoSizing = true;
			nameText.fontSize = 72f * scale;
			nameText.fontSizeMin = 52f * scale;
			nameText.fontSizeMax = 82f * scale;
			nameText.overflowMode = TextOverflowModes.Truncate;
		}
		ApplyTextStyle(priceText, statsCenter, new Vector2(620f * scale, 420f * scale), 68f * scale, 54f * scale, 82f * scale, FontStyles.Bold, TextAlignmentOptions.Center, DossierInkColor);
		ApplyStatsFont(priceText);
		if (priceText != null)
		{
			ApplyDossierTextContrast(priceText, DossierInkColor, 0.16f);
			priceText.fontSize = 30f * scale;
			priceText.fontSizeMin = 22f * scale;
			priceText.fontSizeMax = 38f * scale;
			priceText.margin = new Vector4(12f * scale, 4f * scale, 12f * scale, 4f * scale);
			priceText.lineSpacing = 6f * scale;
			priceText.textWrappingMode = TextWrappingModes.Normal;
			priceText.overflowMode = TextOverflowModes.Overflow;
		}
		if (dossierDescriptionText != null)
		{
			ApplyTextStyle(dossierDescriptionText, new Vector2(dossierCenter.x + 55f * scale, dossierCenter.y - 92f * scale), new Vector2(820f * scale, 440f * scale), 46f * scale, 34f * scale, 54f * scale, FontStyles.Bold, TextAlignmentOptions.Center, DossierInkColor);
			ApplyCharacterNameFont(dossierDescriptionText);
			ApplyDossierTextContrast(dossierDescriptionText, DossierMutedInkColor, 0.14f);
			dossierDescriptionText.fontSize = 40f * scale;
			dossierDescriptionText.fontSizeMin = 30f * scale;
			dossierDescriptionText.fontSizeMax = 50f * scale;
			dossierDescriptionText.margin = new Vector4(24f * scale, 14f * scale, 24f * scale, 14f * scale);
			dossierDescriptionText.lineSpacing = 6f * scale;
			dossierDescriptionText.textWrappingMode = TextWrappingModes.Normal;
			dossierDescriptionText.overflowMode = TextOverflowModes.Truncate;
			dossierDescriptionText.gameObject.SetActive(value: true);
		}
		ApplyDossierPurchasePriceLayout(characterData, new Vector2(dossierCenter.x + 55f * scale, (0f - windowSize.y) * 0.63f), scale);
		if (dossierStatsRule != null)
		{
			ApplyCenteredRect(dossierStatsRule.rectTransform, new Vector2(photoCenter.x, statsCenter.y + 214f * scale), new Vector2(500f * scale, 24f * scale));
			ApplyDossierPartSprite(dossierStatsRule, LoadDossierSprite(ref cachedDossierThinPanelSprite, DossierThinPanelSpritePath), DossierSpriteColor);
			dossierStatsRule.gameObject.SetActive(value: true);
		}
		if (dossierAccentRule != null)
		{
			ApplyCenteredRect(dossierAccentRule.rectTransform, new Vector2(0f, (0f - windowSize.y) * 0.43f), new Vector2(windowSize.x * 0.8f, 24f * scale));
			ApplyDossierPartSprite(dossierAccentRule, LoadDossierSprite(ref cachedDossierThinPanelSprite, DossierThinPanelSpritePath), DossierSpriteColor);
			dossierAccentRule.gameObject.SetActive(value: true);
		}
		BringCardInfoToFront(dossierPaper);
		BringCardInfoToFront(dossierPhotoMat);
		BringCardInfoToFront(iconImage);
		BringCardInfoToFront(dossierAvatarFrameOverlay);
		BringCardInfoToFront(dossierTab);
		BringCardInfoToFront(nameText);
		BringCardInfoToFront(priceText);
		BringCardInfoToFront(dossierDescriptionText);
		BringCardInfoToFront(dossierPurchasePriceText);
		BringCardInfoToFront(dossierPurchaseCurrencyIcon);
		BringCardInfoToFront(dossierStatsRule);
		BringCardInfoToFront(dossierAccentRule);
	}

	private void ApplyDossierProfileRailCardLayout(RectTransform rectTransform, Vector2 size, BattleCharacterDatabase.BattleCharacterData characterData)
	{
		EnsureDossierChrome(rectTransform);
		if (dossierFolderBack != null)
		{
			ApplyCenteredRect(dossierFolderBack.rectTransform, Vector2.zero, size);
			ApplyDossierPartSprite(dossierFolderBack, LoadDossierSprite(ref cachedDossierWindowSprite, "Mahjong/Sprites/BattleLobbyParts/WindowForBattleLobby"), DossierSpriteColor);
			dossierFolderBack.gameObject.SetActive(value: true);
			dossierFolderBack.transform.SetAsFirstSibling();
		}
		if (dossierProfileWindow != null)
		{
			ApplyCenteredRect(dossierProfileWindow.rectTransform, Vector2.zero, size - new Vector2(18f, 18f));
			BattlePopupStyle.ApplyWindow(dossierProfileWindow, raycastTarget: false);
			dossierProfileWindow.gameObject.SetActive(value: true);
		}
		if (dossierTab != null)
		{
			dossierTab.gameObject.SetActive(value: false);
		}
		if (dossierPaper != null)
		{
			dossierPaper.gameObject.SetActive(value: false);
		}
		if (dossierPhotoMat != null)
		{
			ApplyCenteredRect(dossierPhotoMat.rectTransform, new Vector2((0f - size.x) * 0.26f, 0f), new Vector2(size.y * 0.74f, size.y * 0.74f));
			ApplyDossierPartSprite(dossierPhotoMat, LoadDossierSprite(ref cachedDossierInfoPanelSprite, "Mahjong/Sprites/BattleLobbyUI/InfoPanel"), DossierSpriteColor);
			dossierPhotoMat.gameObject.SetActive(value: true);
		}
		if (iconImage != null)
		{
			Sprite sprite = ResolveDossierPortraitSprite(characterData);
			ApplyCenteredRect(iconImage.rectTransform, new Vector2((0f - size.x) * 0.26f, 0f), new Vector2(size.y * 0.58f, size.y * 0.58f));
			if (sprite != null)
			{
				ApplyPortraitSprite(iconModelView, iconImage, sprite);
			}
			else
			{
				HideAvatar(iconModelView, iconImage);
			}
			iconImage.preserveAspect = true;
			iconImage.raycastTarget = false;
		}
		if (dossierAvatarFrameOverlay != null)
		{
			ApplyCenteredRect(dossierAvatarFrameOverlay.rectTransform, new Vector2((0f - size.x) * 0.26f, 0f), new Vector2(size.y * 0.76f, size.y * 0.76f));
			ApplyAvatarFrameSprite(dossierAvatarFrameOverlay);
			dossierAvatarFrameOverlay.gameObject.SetActive(value: true);
		}
		ApplyTextStyle(nameText, new Vector2(size.x * 0.2f, size.y * 0.15f), new Vector2(size.x * 0.42f, 46f), 23f, 17f, 27f, FontStyles.Bold, TextAlignmentOptions.Center, DossierGoldColor);
		ApplyCharacterNameFont(nameText);
		if (nameText != null)
		{
			ApplyDossierTextContrast(nameText, DossierGoldColor, 0.18f);
			nameText.overflowMode = TextOverflowModes.Truncate;
		}
		ApplyTextStyle(priceText, new Vector2(size.x * 0.2f, -40f), new Vector2(size.x * 0.42f, 74f), 15f, 11f, 18f, FontStyles.Bold, TextAlignmentOptions.Center, DossierInkColor);
		ApplyStatsFont(priceText);
		if (priceText != null)
		{
			ApplyDossierTextContrast(priceText, DossierInkColor, 0.15f);
			priceText.text = BuildDossierRailProfileText(characterData);
			priceText.overflowMode = TextOverflowModes.Truncate;
		}
		if (dossierDescriptionText != null)
		{
			dossierDescriptionText.gameObject.SetActive(value: false);
		}
		if (dossierPurchasePriceText != null)
		{
			dossierPurchasePriceText.gameObject.SetActive(value: false);
		}
		if (dossierPurchaseCurrencyIcon != null)
		{
			dossierPurchaseCurrencyIcon.gameObject.SetActive(value: false);
		}
		if (dossierStatsRule != null)
		{
			dossierStatsRule.gameObject.SetActive(value: false);
		}
		if (dossierAccentRule != null)
		{
			dossierAccentRule.gameObject.SetActive(value: false);
		}
		BringCardInfoToFront(dossierProfileWindow);
		BringCardInfoToFront(dossierPaper);
		BringCardInfoToFront(dossierPhotoMat);
		BringCardInfoToFront(iconImage);
		BringCardInfoToFront(dossierAvatarFrameOverlay);
		BringCardInfoToFront(nameText);
		BringCardInfoToFront(priceText);
		BringCardInfoToFront(dossierAccentRule);
	}

	private void EnsureDossierChrome(RectTransform root)
	{
		if (!(root == null))
		{
			dossierFolderBack = EnsureChildImage("DossierFolderBack", root);
			dossierProfileWindow = EnsureChildImage("DossierProfileWindow", root);
			dossierTab = EnsureChildImage("DossierFolderTab", root);
			dossierPaper = EnsureChildImage("DossierPaper", root);
			dossierPhotoMat = EnsureChildImage("DossierPhotoMat", root);
			dossierAvatarFrameOverlay = EnsureChildImage("DossierAvatarFrameOverlay", root);
			dossierStatsRule = EnsureChildImage("DossierStatsRule", root);
			dossierAccentRule = EnsureChildImage("DossierAccentRule", root);
			dossierDescriptionText = EnsureChildText("DossierDescriptionText", root);
			dossierPurchasePriceText = EnsureChildText("DossierPurchasePriceText", root);
			dossierPurchaseCurrencyIcon = EnsureChildImage("DossierPurchaseCurrencyIcon", root);
		}
	}

	private void ApplyDossierPurchasePriceLayout(BattleCharacterDatabase.BattleCharacterData data, Vector2 center, float scale)
	{
		if (dossierPurchasePriceText == null || dossierPurchaseCurrencyIcon == null)
		{
			return;
		}
		bool unlocked = IsCharacterUnlocked(data);
		if (data == null || !data.IsEnabled || unlocked)
		{
			dossierPurchasePriceText.gameObject.SetActive(value: false);
			dossierPurchaseCurrencyIcon.gameObject.SetActive(value: false);
			return;
		}
		int price = ResolveUnlockPrice(data);
		if (price <= 0)
		{
			ApplyTextStyle(dossierPurchasePriceText, center, new Vector2(420f * scale, 72f * scale), 42f * scale, 32f * scale, 50f * scale, FontStyles.Bold, TextAlignmentOptions.Center, DossierGoldColor);
			ApplyCharacterNameFont(dossierPurchasePriceText);
			ApplyDossierTextContrast(dossierPurchasePriceText, DossierGoldColor, 0.18f);
			dossierPurchasePriceText.text = T("battle.character.free", "Free");
			dossierPurchasePriceText.overflowMode = TextOverflowModes.Truncate;
			dossierPurchasePriceText.gameObject.SetActive(value: true);
			dossierPurchaseCurrencyIcon.gameObject.SetActive(value: false);
			return;
		}
		ApplyTextStyle(dossierPurchasePriceText, center + new Vector2(58f * scale, 0f), new Vector2(300f * scale, 110f * scale), 82f * scale, 72f * scale, 92f * scale, FontStyles.Bold, TextAlignmentOptions.Left, DossierGoldColor);
		ApplyCharacterNameFont(dossierPurchasePriceText);
		ApplyDossierTextContrast(dossierPurchasePriceText, DossierGoldColor, 0.18f);
		dossierPurchasePriceText.text = FormatPrice(price);
		dossierPurchasePriceText.overflowMode = TextOverflowModes.Truncate;
		dossierPurchasePriceText.gameObject.SetActive(value: true);
		Sprite icon = LoadCurrencyIcon(ResolveUnlockCurrency(data));
		if (icon != null)
		{
			ApplyCenteredRect(dossierPurchaseCurrencyIcon.rectTransform, center + new Vector2(-54f * scale, 0f), new Vector2(62f * scale, 62f * scale));
			dossierPurchaseCurrencyIcon.sprite = icon;
			dossierPurchaseCurrencyIcon.preserveAspect = true;
			dossierPurchaseCurrencyIcon.color = Color.white;
			dossierPurchaseCurrencyIcon.raycastTarget = false;
			dossierPurchaseCurrencyIcon.gameObject.SetActive(value: true);
		}
		else
		{
			dossierPurchaseCurrencyIcon.gameObject.SetActive(value: false);
		}
	}

	private static void ApplyCenteredRect(RectTransform rect, Vector2 position, Vector2 size)
	{
		if (!(rect == null))
		{
			rect.anchorMin = new Vector2(0.5f, 0.5f);
			rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.anchoredPosition = position;
			rect.sizeDelta = size;
			rect.localScale = Vector3.one;
		}
	}

	private static Vector2 ResolveFullscreenDossierSize(RectTransform root)
	{
		RectTransform rectTransform = ((root != null) ? (root.GetComponentInParent<Canvas>()?.transform as RectTransform) : null);
		Vector2 vector = ((rectTransform != null) ? rectTransform.rect.size : Vector2.zero);
		if (vector.x <= 1f || vector.y <= 1f)
		{
			RectTransform rectTransform2 = ((root != null) ? (root.parent as RectTransform) : null);
			vector = ((rectTransform2 != null) ? rectTransform2.rect.size : Vector2.zero);
		}
		if (vector.x <= 1f || vector.y <= 1f)
		{
			return DossierCardSize;
		}
		return new Vector2(Mathf.Max(DossierMinFullscreenSize.x, vector.x), Mathf.Max(DossierMinFullscreenSize.y, vector.y));
	}

	private static float ResolveDossierScaleFactor(Vector2 cardSize)
	{
		float a = cardSize.x / DossierCardSize.x;
		float b = cardSize.y / DossierCardSize.y;
		return Mathf.Clamp(Mathf.Min(a, b), 0.76f, 1.24f);
	}

	private static Vector2 ScaleVector(Vector2 value, float scale)
	{
		return value * scale;
	}

	private static void ApplyDossierPartSprite(Image image, Sprite sprite, Color tint)
	{
		if (!(image == null))
		{
			if (sprite == null)
			{
				image.enabled = false;
				image.sprite = null;
				image.raycastTarget = false;
				return;
			}
			image.enabled = true;
			image.sprite = sprite;
			image.type = ((sprite.border.sqrMagnitude > 0.01f) ? Image.Type.Sliced : Image.Type.Simple);
			image.preserveAspect = false;
			image.raycastTarget = false;
			image.color = tint;
		}
	}

	private static void ApplyAvatarFrameSprite(Image image)
	{
		if (image == null)
		{
			return;
		}
		Sprite sprite = LoadDossierSprite(ref cachedDossierAvatarFrameSprite, DossierAvatarFrameSpritePath);
		if (sprite == null)
		{
			image.enabled = false;
			image.sprite = null;
			image.raycastTarget = false;
			return;
		}
		image.enabled = true;
		image.sprite = sprite;
		image.type = Image.Type.Simple;
		image.preserveAspect = true;
		image.raycastTarget = false;
		image.color = DossierSpriteColor;
	}

	private static Sprite LoadDossierSprite(ref Sprite cache, string path)
	{
		if (cache != null)
		{
			return cache;
		}
		cache = Resources.Load<Sprite>(path);
		if (cache != null)
		{
			return cache;
		}
		Sprite[] array = Resources.LoadAll<Sprite>(path);
		if (array == null || array.Length == 0)
		{
			return null;
		}
		cache = array[0];
		float num = cache.rect.width * cache.rect.height;
		for (int i = 1; i < array.Length; i++)
		{
			Sprite sprite = array[i];
			if (!(sprite == null))
			{
				float num2 = sprite.rect.width * sprite.rect.height;
				if (!(num2 <= num))
				{
					cache = sprite;
					num = num2;
				}
			}
		}
		return cache;
	}

	private static Sprite LoadCurrencyIcon(BattleCharacterDatabase.CharacterPriceCurrencyType currency)
	{
		switch (currency)
		{
			case BattleCharacterDatabase.CharacterPriceCurrencyType.OzAmetist:
				return LoadDossierSprite(ref cachedOzAmetistIconSprite, OzAmetistIconSpritePath);
			case BattleCharacterDatabase.CharacterPriceCurrencyType.OzAltin:
				return LoadDossierSprite(ref cachedOzAltinIconSprite, OzAltinIconSpritePath);
			case BattleCharacterDatabase.CharacterPriceCurrencyType.OzTile:
			default:
				return LoadDossierSprite(ref cachedOzTileIconSprite, OzTileIconSpritePath);
		}
	}

	private static void ApplyDossierTextContrast(TMP_Text text, Color color, float outlineWidth)
	{
		if (!(text == null))
		{
			text.color = color;
			text.outlineColor = new Color(0f, 0f, 0f, 0.92f);
			text.outlineWidth = outlineWidth;
			text.margin = new Vector4(8f, 4f, 8f, 4f);
		}
	}

	private static void ApplyTextStyle(TMP_Text text, Vector2 position, Vector2 size, float fontSize, float minSize, float maxSize, FontStyles fontStyle, TextAlignmentOptions alignment, Color color)
	{
		if (!(text == null))
		{
			ApplyCenteredRect(text.rectTransform, position, size);
			text.alignment = alignment;
			text.fontStyle = fontStyle;
			text.enableAutoSizing = true;
			text.fontSize = fontSize;
			text.fontSizeMin = minSize;
			text.fontSizeMax = maxSize;
			text.overflowMode = TextOverflowModes.Truncate;
			text.textWrappingMode = TextWrappingModes.Normal;
			text.richText = true;
			text.color = color;
			text.raycastTarget = false;
		}
	}

	private void BringCardInfoToFront(Component component)
	{
		if (!(component == null))
		{
			Transform transform = base.transform;
			Transform parent = component.transform;
			while (parent.parent != null && parent.parent != transform)
			{
				parent = parent.parent;
			}
			if (parent.parent == transform)
			{
				parent.SetAsLastSibling();
			}
		}
	}

	private static void ApplyCharacterNameFont(TMP_Text text)
	{
		if (!(text == null))
		{
			TMP_FontAsset tMP_FontAsset = ResolveCharacterNameFont();
			if (tMP_FontAsset != null)
			{
				text.font = tMP_FontAsset;
				text.fontSharedMaterial = tMP_FontAsset.material;
			}
			text.fontStyle = FontStyles.Bold;
			text.enableAutoSizing = true;
			text.color = new Color(1f, 0.82f, 0.28f, 1f);
			text.fontSize = 30f;
			text.fontSizeMin = 16f;
			text.fontSizeMax = 32f;
			text.margin = Vector4.zero;
			text.overflowMode = TextOverflowModes.Truncate;
			text.textWrappingMode = TextWrappingModes.Normal;
			text.alignment = TextAlignmentOptions.Center;
			text.raycastTarget = false;
		}
	}

	private static void ApplyStatsFont(TMP_Text text)
	{
		if (!(text == null))
		{
			TMP_FontAsset tMP_FontAsset = ResolveCharacterNameFont();
			if (tMP_FontAsset != null)
			{
				text.font = tMP_FontAsset;
				text.fontSharedMaterial = tMP_FontAsset.material;
			}
			text.fontStyle = FontStyles.Bold;
			text.enableAutoSizing = true;
			text.fontSize = 21f;
			text.fontSizeMin = 13f;
			text.fontSizeMax = 24f;
			text.margin = Vector4.zero;
			text.overflowMode = TextOverflowModes.Truncate;
			text.textWrappingMode = TextWrappingModes.Normal;
			text.alignment = TextAlignmentOptions.Center;
			text.richText = true;
			text.raycastTarget = false;
		}
	}

	private static TMP_FontAsset ResolveCharacterNameFont()
	{
		if (cachedCharacterNameFont != null)
		{
			return cachedCharacterNameFont;
		}
		cachedCharacterNameFont = BattlePopupStyle.Font ?? MainLobbyButtonStyle.Font;
		return cachedCharacterNameFont;
	}

	private BattleCharacterDatabase.BattleCharacterData GetCharacterData()
	{
		if (string.IsNullOrWhiteSpace(characterId))
		{
			return null;
		}
		BattleCharacterDatabase battleCharacterDatabase = (BattleCharacterDatabase.HasInstance ? BattleCharacterDatabase.Instance : UnityEngine.Object.FindAnyObjectByType<BattleCharacterDatabase>(FindObjectsInactive.Include));
		if (battleCharacterDatabase == null)
		{
			return null;
		}
		BattleCharacterDatabase.BattleCharacterData characterOrNull = battleCharacterDatabase.GetCharacterOrNull(characterId);
		if (characterOrNull != null)
		{
			return characterOrNull;
		}
		battleCharacterDatabase.RebuildCache();
		return battleCharacterDatabase.GetCharacterOrNull(characterId);
	}

	private bool IsCharacterUnlocked(BattleCharacterDatabase.BattleCharacterData data)
	{
		if (data == null || !data.IsEnabled)
		{
			return false;
		}
		if (BattleCharacterSelectionService.HasInstance)
		{
			return BattleCharacterSelectionService.Instance.IsUnlocked(characterId);
		}
		if (!data.IsStarterFree)
		{
			return data.UnlockType == BattleCharacterDatabase.CharacterUnlockType.Default;
		}
		return true;
	}

	private bool IsCharacterSelected()
	{
		if (!BattleCharacterSelectionService.HasInstance)
		{
			return false;
		}
		return string.Equals(BattleCharacterSelectionService.Instance.SelectedCharacterId, characterId, StringComparison.Ordinal);
	}

	private bool HasSelectedCharacter()
	{
		if (!BattleCharacterSelectionService.HasInstance)
		{
			return false;
		}
		return BattleCharacterSelectionService.Instance.HasSelectedCharacter();
	}

	private void PushPreviewIfNeeded()
	{
		if (IsCharacterSelected())
		{
			UpdatePreviewWindow();
		}
		else if (autoUseThisCharacterAsPreviewIfNothingSelected && !HasSelectedCharacter())
		{
			UpdatePreviewWindow();
		}
	}

	private void ApplyImage(Image target, Sprite sprite)
	{
		if (!(target == null))
		{
			target.sprite = sprite;
			target.enabled = sprite != null;
			SetImageAlpha(target, (sprite != null) ? 1f : 0f);
		}
	}

	private void ApplyPreviewModelOrImage(ref BattleCharacterModelView modelView, Image image, BattleCharacterDatabase.BattleCharacterData data, BattleCharacterModelView.ModelContext modelContext, Sprite fallbackSprite)
	{
		if (!ApplyModel(modelView, image, data, modelContext))
		{
			ApplyImage(image, fallbackSprite);
		}
		if (modelView == null && image != null)
		{
			modelView = image.GetComponent<BattleCharacterModelView>();
		}
	}

	private void HideAvatar(BattleCharacterModelView modelView, Image image)
	{
		if (modelView == null && image != null)
		{
			modelView = image.GetComponent<BattleCharacterModelView>();
		}
		if (modelView != null)
		{
			modelView.Hide();
			modelView.enabled = false;
		}
		if (!(image == null))
		{
			image.sprite = null;
			image.enabled = false;
			SetImageAlpha(image, 0f);
		}
	}

	private void ApplyPortraitSprite(BattleCharacterModelView modelView, Image image, Sprite sprite)
	{
		if (modelView == null && image != null)
		{
			modelView = image.GetComponent<BattleCharacterModelView>();
		}
		if (modelView != null)
		{
			modelView.Hide();
			modelView.enabled = false;
		}
		if (image == null)
		{
			return;
		}
		BattleCharacterModelView[] componentsInChildren = image.GetComponentsInChildren<BattleCharacterModelView>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (!(componentsInChildren[i] == null))
			{
				componentsInChildren[i].Hide();
				componentsInChildren[i].enabled = false;
			}
		}
		RawImage[] componentsInChildren2 = image.GetComponentsInChildren<RawImage>(includeInactive: true);
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			if (!(componentsInChildren2[j] == null))
			{
				componentsInChildren2[j].texture = null;
				componentsInChildren2[j].enabled = false;
			}
		}
		image.sprite = sprite;
		image.enabled = sprite != null;
		image.preserveAspect = true;
		SetImageAlpha(image, (sprite != null) ? 1f : 0f);
	}

	private static Sprite ResolveDossierPortraitSprite(BattleCharacterDatabase.BattleCharacterData data)
	{
		if (data == null)
		{
			return null;
		}
		Sprite sprite = ((data.SelectSprite != null) ? data.SelectSprite : ((data.LobbySprite != null) ? data.LobbySprite : data.BattleSprite));
		if (sprite != null)
		{
			return sprite;
		}
		return ResolveRuntimeAvatarSprite(data);
	}

	private static Sprite ResolveRuntimeAvatarSprite(BattleCharacterDatabase.BattleCharacterData data)
	{
		if (data == null || string.IsNullOrWhiteSpace(data.Id))
		{
			return null;
		}
		string text = data.Id.Trim().Replace("_", string.Empty);
		string[] array = new string[3]
		{
			text + "Foto",
			data.Id.Trim() + "Foto",
			$"{data.AnimalType}{data.Gender}Foto"
		};
		for (int i = 0; i < array.Length; i++)
		{
			Sprite sprite = Resources.Load<Sprite>("BattleCharacters/Avatars/" + array[i]);
			if (sprite != null)
			{
				return sprite;
			}
			Texture2D texture2D = Resources.Load<Texture2D>("BattleCharacters/Avatars/" + array[i]);
			if (!(texture2D == null))
			{
				sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				sprite.name = array[i];
				return sprite;
			}
		}
		return null;
	}

	private bool ApplyModel(BattleCharacterModelView modelView, Image image, BattleCharacterDatabase.BattleCharacterData data, BattleCharacterModelView.ModelContext modelContext)
	{
		if (image == null || data == null)
		{
			if (modelView != null)
			{
				modelView.Hide();
			}
			return false;
		}
		if (modelView == null)
		{
			modelView = EnsureModelView(image);
		}
		if (modelView != null)
		{
			modelView.enabled = true;
		}
		if (modelView != null)
		{
			return modelView.Show(data, modelContext);
		}
		return false;
	}

	private BattleCharacterModelView EnsureModelView(Image image)
	{
		if (image == null)
		{
			return null;
		}
		BattleCharacterModelView battleCharacterModelView = image.GetComponent<BattleCharacterModelView>();
		if (battleCharacterModelView == null)
		{
			battleCharacterModelView = image.gameObject.AddComponent<BattleCharacterModelView>();
		}
		return battleCharacterModelView;
	}

	private void SetImageAlpha(Image image, float alpha)
	{
		if (!(image == null))
		{
			Color color = image.color;
			color.a = alpha;
			image.color = color;
		}
	}

	private void Subscribe()
	{
		if (!subscribed && BattleCharacterSelectionService.HasInstance)
		{
			BattleCharacterSelectionService.Instance.SelectedCharacterChanged += OnSelectedCharacterChanged;
			BattleCharacterSelectionService.Instance.SelectionStateChanged += OnSelectionStateChanged;
			subscribed = true;
		}
	}

	private void Unsubscribe()
	{
		if (subscribed)
		{
			if (BattleCharacterSelectionService.HasInstance)
			{
				BattleCharacterSelectionService.Instance.SelectedCharacterChanged -= OnSelectedCharacterChanged;
				BattleCharacterSelectionService.Instance.SelectionStateChanged -= OnSelectionStateChanged;
			}
			subscribed = false;
		}
	}

	private void OnSelectedCharacterChanged(string _)
	{
		if (!IsInvalidForCallbacks())
		{
			Refresh();
		}
	}

	private void OnSelectionStateChanged()
	{
		if (!IsInvalidForCallbacks())
		{
			Refresh();
		}
	}

	private void OnLanguageChanged(GameLanguage language)
	{
		if (IsInvalidForCallbacks())
		{
			AppSettings.OnLanguageChanged -= OnLanguageChanged;
			return;
		}
		Refresh();
		if (ownerCarousel == null || ownerCarousel.CenteredButton == this || IsCharacterSelected())
		{
			UpdatePreviewWindow();
		}
	}

	private bool IsInvalidForCallbacks()
	{
		if (!isDestroying)
		{
			return this == null;
		}
		return true;
	}

	private void ApplyMissingState()
	{
		ApplyCardBackground(selected: false);
		if (iconImage != null)
		{
			iconImage.sprite = null;
			iconImage.enabled = false;
			SetImageAlpha(iconImage, 0f);
		}
		if (nameText != null)
		{
			nameText.text = "N/A";
		}
		if (priceText != null)
		{
			priceText.text = string.Empty;
		}
		UpdateCardStatsVisibility();
		if (lockedRoot != null)
		{
			lockedRoot.SetActive(value: false);
		}
		if (selectedRoot != null)
		{
			selectedRoot.SetActive(value: false);
		}
		if (disabledRoot != null)
		{
			disabledRoot.SetActive(value: true);
		}
		if (button != null)
		{
			button.interactable = false;
		}
		if (scaleTarget != null)
		{
			scaleTarget.localScale = normalScale;
		}
	}

	private string BuildPriceText(BattleCharacterDatabase.BattleCharacterData data, bool unlocked)
	{
		if (data == null)
		{
			return string.Empty;
		}
		if (!data.IsEnabled)
		{
			return T("battle.character.disabled", "Disabled");
		}
		if (unlocked)
		{
			return T("battle.character.unlocked", "Unlocked");
		}
		if (BattleCharacterSelectionService.HasInstance)
		{
			int unlockPrice = BattleCharacterSelectionService.Instance.GetUnlockPrice(data.Id);
			if (unlockPrice > 0)
			{
				return FormatPriceRich(unlockPrice, ResolveUnlockCurrency(data));
			}
			return T("battle.character.free", "Free");
		}
		if (data.IsStarterFree || data.UnlockType == BattleCharacterDatabase.CharacterUnlockType.Default)
		{
			return T("battle.character.free", "Free");
		}
		string text = string.Empty;
		switch (data.PriceCurrency)
		{
		case BattleCharacterDatabase.CharacterPriceCurrencyType.OzAltin:
			text = GetGoldName();
			break;
		case BattleCharacterDatabase.CharacterPriceCurrencyType.OzAmetist:
			text = "Oz Ametist";
			break;
		case BattleCharacterDatabase.CharacterPriceCurrencyType.OzTile:
			text = "OzTile";
			break;
		}
		if (string.IsNullOrEmpty(text) || data.PriceAmount <= 0)
		{
			return T("battle.character.locked", "Locked");
		}
		return "<b><color=#" + ColorUtility.ToHtmlStringRGB(statsPriceColor) + ">" + FormatPrice(data.PriceAmount) + " " + text + "</color></b>";
	}

	private void ApplyCardBackground(bool selected)
	{
		if (!ensureVisibleCardBackground)
		{
			return;
		}
		Image image = GetComponent<Image>();
		if (image == null)
		{
			image = base.gameObject.AddComponent<Image>();
		}
		if (applyBattleLobbyReleaseCardStyle)
		{
			if (applyBattleLobbyDossierCardStyle)
			{
				image.sprite = null;
				image.color = new Color(0f, 0f, 0f, 0f);
			}
			else
			{
				BattlePopupStyle.ApplyWindow(image, raycastTarget: false);
				image.color = (selected ? new Color(1f, 0.86f, 0.48f, 1f) : new Color(0.74f, 0.62f, 0.38f, 0.96f));
				ApplyReleaseCardSelectionState(selected);
			}
		}
		else
		{
			image.color = (selected ? selectedCardBackgroundColor : cardBackgroundColor);
		}
		image.raycastTarget = true;
		if (button != null && button.targetGraphic == null)
		{
			button.targetGraphic = image;
		}
	}

	private void EnsureReleaseCardChrome()
	{
		if (applyBattleLobbyReleaseCardStyle)
		{
			if (releaseCardFront != null)
			{
				releaseCardFront.enabled = false;
				releaseCardFront.gameObject.SetActive(value: false);
			}
			if (releasePortraitGlow != null)
			{
				releasePortraitGlow.enabled = false;
				releasePortraitGlow.gameObject.SetActive(value: false);
			}
		}
	}

	private static Image EnsureChildImage(string objectName, RectTransform parent)
	{
		Transform transform = parent.Find(objectName);
		GameObject gameObject = ((transform != null) ? transform.gameObject : new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)));
		if (transform == null)
		{
			gameObject.transform.SetParent(parent, worldPositionStays: false);
		}
		Image image = gameObject.GetComponent<Image>();
		if (image == null)
		{
			image = gameObject.AddComponent<Image>();
		}
		image.raycastTarget = false;
		return image;
	}

	private static TMP_Text EnsureChildText(string objectName, RectTransform parent)
	{
		Transform transform = parent.Find(objectName);
		GameObject gameObject = ((transform != null) ? transform.gameObject : new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI)));
		if (transform == null)
		{
			gameObject.transform.SetParent(parent, worldPositionStays: false);
		}
		TMP_Text tMP_Text = gameObject.GetComponent<TMP_Text>();
		if (tMP_Text == null)
		{
			tMP_Text = gameObject.AddComponent<TextMeshProUGUI>();
		}
		tMP_Text.raycastTarget = false;
		return tMP_Text;
	}

	private void ApplyReleaseCardSelectionState(bool selected)
	{
		if (applyBattleLobbyReleaseCardStyle)
		{
			if (releaseCardFront != null)
			{
				releaseCardFront.gameObject.SetActive(value: false);
			}
			if (releasePortraitGlow != null)
			{
				releasePortraitGlow.gameObject.SetActive(value: false);
			}
		}
	}

	private void UpdateCardStatsVisibility()
	{
		if (priceText != null && !priceText.gameObject.activeSelf)
		{
			priceText.gameObject.SetActive(value: true);
		}
	}

	private void RefreshCardStatsText()
	{
		if (!(priceText == null) && !IsInvalidForCallbacks())
		{
			BattleCharacterDatabase.BattleCharacterData characterData = GetCharacterData();
			if (characterData != null)
			{
				RefreshCardStatsText(characterData, IsCharacterUnlocked(characterData));
			}
		}
	}

	private void RefreshCardStatsText(BattleCharacterDatabase.BattleCharacterData data, bool unlocked)
	{
		if (!(priceText == null))
		{
			ApplyStatsFont(priceText);
			if (applyBattleLobbyDossierCardStyle)
			{
				bool flag = isHighlighted || ownerCarousel == null;
				ApplyDossierTextContrast(priceText, flag ? DossierInkColor : DossierGoldColor, flag ? 0.16f : 0.14f);
			}
			else
			{
				priceText.color = statsTextColor;
			}
			bool includeStats = !showStatsOnlyWhenHighlighted || isHighlighted || ownerCarousel == null;
			priceText.text = BuildCardStatsText(data, unlocked, includeStats);
		}
	}

	private string BuildCardStatsText(BattleCharacterDatabase.BattleCharacterData data, bool unlocked, bool includeStats)
	{
		if (data == null)
		{
			return string.Empty;
		}
		if (applyBattleLobbyDossierCardStyle)
		{
			return BuildDossierStatsText(data, unlocked, includeStats);
		}
		string text = T("battle.character.stat.hp", "HP");
		string text2 = T("battle.character.stat.attack_short", "ATK");
		string text3 = T("battle.character.stat.armor_short", "ARM");
		string text4 = T("battle.character.stat.crit", "CRIT");
		string text5 = $"{Mathf.RoundToInt(data.Stats.Armor * 100f)}%";
		string text6 = $"{Mathf.RoundToInt(data.Stats.CritChance * 100f)}%";
		string text7 = BuildCardFooterText(data, unlocked);
		if (!includeStats)
		{
			if (!string.IsNullOrEmpty(text7))
			{
				return text7;
			}
			return T("battle.character.unlocked", "Unlocked");
		}
		if (!useStatsBackdrop)
		{
			return $"{text} {data.Stats.MaxHp}   {text2} {data.Stats.Attack}\n" + text3 + " " + text5 + "   " + text4 + " " + text6 + (string.IsNullOrEmpty(text7) ? string.Empty : ("\n" + text7));
		}
		string text8 = ColorUtility.ToHtmlStringRGBA(statsBackdropColor);
		return $"<mark=#{text8}> {text} {data.Stats.MaxHp}   {text2} {data.Stats.Attack}   {text4} {text6} </mark>\n" + "<mark=#" + text8 + "> " + text3 + " " + text5 + " </mark>" + (string.IsNullOrEmpty(text7) ? string.Empty : ("\n" + text7));
	}

	private string BuildDossierStatsText(BattleCharacterDatabase.BattleCharacterData data, bool unlocked, bool includeStats)
	{
		string text = LocalText("Здоровье", "Health", "Can", "Gesundheit");
		string text2 = LocalText("Атака", "Attack", "Saldırı", "Angriff");
		string text3 = LocalText("Броня", "Armor", "Zırh", "Ruestung");
		string text4 = LocalText("Крит", "Crit", "Kritik", "Krit");
		string text5 = LocalText("Крит урон", "Crit Damage", "Kritik Hasar", "Kritischer Schaden");
		string text8 = LocalText("Класс", "Class", "Sınıf", "Klasse") + ": " + GetDossierClassName(data);
		if (!includeStats)
		{
			return text8;
		}
		string text9 = "<voffset=-0.22em>" + text8 + "</voffset>\n"
			+ $"{text}: {data.Stats.MaxHp}\n"
			+ $"{text2}: {data.Stats.Attack}\n"
			+ $"{text3}: {Mathf.RoundToInt(data.Stats.Armor * 100f)}%\n"
			+ $"{text4}: {Mathf.RoundToInt(data.Stats.CritChance * 100f)}%\n"
			+ $"{text5}: x{data.Stats.CritDamageMultiplier:0.##}";
		return text9;
	}

	private string BuildDossierRailProfileText(BattleCharacterDatabase.BattleCharacterData data)
	{
		if (data == null)
		{
			return string.Empty;
		}
		return LocalText("Класс", "Class", "Sınıf", "Klasse") + ": " + GetDossierClassName(data);
	}

	private void RefreshDossierDescriptionText(BattleCharacterDatabase.BattleCharacterData data, bool unlocked)
	{
		if (applyBattleLobbyDossierCardStyle && !(dossierDescriptionText == null) && data != null)
		{
			dossierDescriptionText.text = BuildDossierDescriptionText(data, unlocked);
		}
	}

	private string BuildDossierDescriptionText(BattleCharacterDatabase.BattleCharacterData data, bool unlocked)
	{
		return LocalText("Династия", "Dynasty", "Hanedan", "Dynastie") + ": " + GetDossierHouseName(data) + "\n" + GetDossierLore(data);
	}

	private static string GetDossierClassName(BattleCharacterDatabase.BattleCharacterData data)
	{
		if (data == null)
		{
			return GameLocalization.Text("battle.character.rail.class.fighter");
		}
		return data.AnimalType switch
		{
			BattleCharacterDatabase.CharacterAnimalType.Tiger => GameLocalization.Text("battle.character.rail.class.vanguard"),
			BattleCharacterDatabase.CharacterAnimalType.Fox => GameLocalization.Text("battle.character.rail.class.scout"),
			BattleCharacterDatabase.CharacterAnimalType.Wolf => GameLocalization.Text("battle.character.rail.class.duelist"),
			BattleCharacterDatabase.CharacterAnimalType.Bear => GameLocalization.Text("battle.character.rail.class.sentinel"),
			BattleCharacterDatabase.CharacterAnimalType.Dragon => GameLocalization.Text("battle.character.rail.class.arcanist"),
			BattleCharacterDatabase.CharacterAnimalType.Dog => GameLocalization.Text("battle.character.rail.class.tracker"),
			_ => GameLocalization.Text("battle.character.rail.class.fighter"),
		};
	}

	private static string GetDossierHouseName(BattleCharacterDatabase.BattleCharacterData data)
	{
		if (data == null)
		{
			return LocalText("Бамбуковый круг", "Bamboo Circle", "Bambu Çemberi", "Bambuskreis");
		}
		return data.AnimalType switch
		{
			BattleCharacterDatabase.CharacterAnimalType.Tiger => LocalText("Дом Пепельного Когтя", "Ash Claw House", "Kül Pençesi Hanesi", "Haus Aschenklaue"),
			BattleCharacterDatabase.CharacterAnimalType.Fox => LocalText("Дом Тихой Искры", "Quiet Spark House", "Sessiz Kıvılcım Hanesi", "Haus Stiller Funke"),
			BattleCharacterDatabase.CharacterAnimalType.Wolf => LocalText("Дом Лунной Тропы", "Moon Trail House", "Ay Yolu Hanesi", "Haus Mondpfad"), 
			BattleCharacterDatabase.CharacterAnimalType.Bear => LocalText("Дом Каменной Чаши", "Stone Bowl House", "Taş Kase Hanesi", "Haus Steinschale"), 
			BattleCharacterDatabase.CharacterAnimalType.Dragon => LocalText("Дом Золотого Дыма", "Golden Smoke House", "Altın Duman Hanesi", "Haus Goldrauch"), 
			BattleCharacterDatabase.CharacterAnimalType.Dog => LocalText("Дом Верного Следа", "True Trail House", "Sadık İz Hanesi", "Haus Treue Spur"),
			_ => LocalText("Бамбуковый круг", "Bamboo Circle", "Bambu Çemberi", "Bambuskreis"),
		};
	}

	private static string GetDossierLore(BattleCharacterDatabase.BattleCharacterData data)
	{
		switch ((data != null && !string.IsNullOrWhiteSpace(data.Id)) ? data.Id.Trim().ToLowerInvariant() : string.Empty)
		{
		case "tiger_male":
			return LocalText("Тигр из Пепельного Когтя пришел на арену после пожара в бамбуковом лесу.", "Tiger of Ash Claw came to the arena after the bamboo forest fire.", "Kül Pençesi Kaplanı, bambu ormanı yangınından sonra arenaya geldi.", "Tiger der Aschenklaue kam nach dem Brand im Bambuswald in die Arena.");
		case "tiger_female":
			return LocalText("Тигрица хранит память о старой засаде и выходит на бой за честь своего дома.", "Tigress carries the memory of an old ambush and fights for her house's honor.", "Dişi Kaplan eski bir pusunun hatırasını taşır ve hanesinin onuru için savaşır.", "Tigerin tragt die Erinnerung an einen alten Hinterhalt und kampft fur die Ehre ihres Hauses.");
		case "fox_male":
			return LocalText("Лис известен как тихий участник чайных споров, где решают судьбу будущих дуэлей.", "Fox is known from quiet tea-house disputes where future duels are decided.", "Tilki, gelecekteki düelloların belirlendiği sessiz çay evi tartışmalarıyla tanınır.", "Fuchs ist aus stillen Teehausstreiten bekannt, in denen kunftige Duelle entschieden werden.");
		case "fox_female":
			return LocalText("Лисица ведет личный список долгов и появляется там, где спор нельзя решить словами.", "Vixen keeps a private ledger of debts and appears where words can no longer settle a dispute.", "Dişi Tilki kendi borç defterini tutar ve sözlerin yetmediği yerde ortaya çıkar.", "Fuechsin fuhrt ein eigenes Schuldbuch und erscheint, wo Worte nicht mehr reichen.");
		case "wolf_male":
			return LocalText("Волк пережил раскол Лунной Тропы и теперь доказывает право носить знак дома.", "Wolf survived the Moon Trail split and now proves his right to wear the house mark.", "Kurt, Ay Yolu bölünmesini atlattı ve hane işaretini taşıma hakkını kanıtlar.", "Wolf uberlebte die Spaltung des Mondpfads und beweist nun sein Recht auf das Hauszeichen.");
		case "wolf_female":
			return LocalText("Волчица несет серебряный жетон Лунной Тропы и принимает вызовы без свидетелей.", "She-Wolf carries the silver token of Moon Trail and accepts challenges without witnesses.", "Dişi Kurt Ay Yolu'nun gümüş nişanesini taşır ve tanıksız meydan okumaları kabul eder.", "Wolfin tragt das silberne Zeichen des Mondpfads und nimmt Duelle ohne Zeugen an.");
		case "bear_male":
			return LocalText("Медведь был хранителем Каменной Чаши, пока пожар не сделал его имя частью арены.", "Bear guarded the Stone Bowl until the fire made his name part of the arena.", "Ayı, ateş adını arenanın parçası yapana kadar Taş Kase'nin koruyucusuydu.", "Bar bewachte die Steinschale, bis das Feuer seinen Namen in die Arena trug.");
		case "bear_female":
			return LocalText("Медведица пришла из северных залов Каменной Чаши, где имена записывают после боя.", "She-Bear came from the northern halls of Stone Bowl, where names are written after battle.", "Dişi Ayı isimlerin savaştan sonra yazıldığı Taş Kase'nin kuzey salonlarından geldi.", "Baerin kam aus den Nordhallen der Steinschale, wo Namen nach dem Kampf geschrieben werden.");
		case "dragon_male":
			return LocalText("Дракон хранит древний счет Золотого Дыма и выходит на арену только по старому долгу.", "Dragon keeps the old account of Golden Smoke and enters the arena only for an ancient debt.", "Ejderha Altın Duman'ın eski hesabını tutar ve arenaya yalnız kadim borç için çıkar.", "Drache huetet die alte Rechnung des Goldrauchs und betritt die Arena nur wegen alter Schuld.");
		case "dragon_female":
			return LocalText("Драконица покинула храм Золотого Дыма, когда ее печать появилась в списке вызовов.", "Dragoness left the Golden Smoke temple when her seal appeared on the challenge list.", "Dişi Ejderha mührü meydan okuma listesinde görününce Altın Duman tapınağından ayrıldı.", "Drachin verliess den Tempel des Goldrauchs, als ihr Siegel auf der Duellliste erschien.");
		case "dog_male":
			return LocalText("Верный из Дома Следа охранял границу леса и вышел на арену после исчезновения каравана.", "Faithful of True Trail guarded the forest border and entered the arena after a caravan vanished.", "Sadık, İz Hanesi'nin orman sınırını korurdu; bir kervan kaybolunca arenaya çıktı.", "Treu aus dem Haus der Spur bewachte die Waldgrenze und betrat die Arena nach dem Verschwinden einer Karawane.");
		case "dog_female":
			return LocalText("Верная из Дома Следа носит медный жетон караула и принимает вызовы за честь дозора.", "Faithful of True Trail carries a copper watch token and accepts challenges for the watch's honor.", "Sadık, bakır nöbet nişanı taşır ve nöbetin onuru için meydan okumaları kabul eder.", "Treu aus dem Haus der Spur trägt eine kupferne Wachtmarke und nimmt Duelle für die Ehre der Wache an.");
		default:
		{
			bool flag = data != null && data.Gender == BattleCharacterDatabase.CharacterGender.Female;
			switch (data?.AnimalType ?? BattleCharacterDatabase.CharacterAnimalType.Tiger)
			{
			case BattleCharacterDatabase.CharacterAnimalType.Tiger:
				if (!flag)
				{
					return LocalText("Тигр носит знак Пепельного Когтя.", "Tiger carries the Ash Claw mark.", "Kaplan, Kül Pençesi işaretini taşır.", "Tiger tragt das Zeichen der Aschenklaue.");
				}
				return LocalText("Имя Тигрицы записано среди бойцов Пепельного Когтя.", "Tigress is listed among the fighters of Ash Claw.", "Dişi Kaplan Kül Pençesi savaşçıları arasında yazılıdır.", "Tigerin steht unter den Kampfern der Aschenklaue.");
			case BattleCharacterDatabase.CharacterAnimalType.Fox:
				if (!flag)
				{
					return LocalText("Лис пришел из дома Тихой Искры.", "Fox came from Quiet Spark House.", "Tilki, Sessiz Kıvılcım Hanesi'nden geldi.", "Fuchs kam aus dem Haus Stiller Funke.");
				}
				return LocalText("Лисица известна в доме Тихой Искры.", "Vixen is known in Quiet Spark House.", "Dişi Tilki Sessiz Kıvılcım Hanesi'nde tanınır.", "Fuechsin ist im Haus Stiller Funke bekannt.");
			case BattleCharacterDatabase.CharacterAnimalType.Wolf:
				if (!flag)
				{
					return LocalText("Волк связан с расколом Лунной Тропы.", "Wolf is tied to the Moon Trail split.", "Kurt, Ay Yolu bölünmesine bağlıdır.", "Wolf ist mit der Spaltung des Mondpfads verbunden.");
				}
				return LocalText("Волчица носит серебро Лунной Тропы.", "She-Wolf wears the silver of Moon Trail.", "Dişi Kurt Ay Yolu gümüşünü taşır.", "Wolfin tragt das Silber des Mondpfads.");
			case BattleCharacterDatabase.CharacterAnimalType.Bear:
				if (!flag)
				{
					return LocalText("Медведь был хранителем Каменной Чаши.", "Bear was a keeper of Stone Bowl.", "Ayı, Taş Kase'nin koruyucusuydu.", "Bar war Huter der Steinschale.");
				}
				return LocalText("Медведица пришла из залов Каменной Чаши.", "She-Bear came from the halls of Stone Bowl.", "Dişi Ayı Taş Kase salonlarından geldi.", "Baerin kam aus den Hallen der Steinschale.");
			case BattleCharacterDatabase.CharacterAnimalType.Dragon:
				if (!flag)
				{
					return LocalText("Дракон хранит старый счет Золотого Дыма.", "Dragon keeps the old account of Golden Smoke.", "Ejderha Altın Duman'ın eski hesabını tutar.", "Drache huetet die alte Rechnung des Goldrauchs.");
				}
				return LocalText("Драконица связана с храмом Золотого Дыма.", "Dragoness is tied to the Golden Smoke temple.", "Dişi Ejderha Altın Duman tapınağına bağlıdır.", "Drachin ist mit dem Tempel des Goldrauchs verbunden.");
			case BattleCharacterDatabase.CharacterAnimalType.Dog:
				if (!flag)
				{
					return LocalText("Верный носит знак дома Верного Следа.", "Faithful carries the mark of True Trail House.", "Sadık, Sadık İz Hanesi'nin işaretini taşır.", "Treu trägt das Zeichen des Hauses Treue Spur.");
				}
				return LocalText("Верная служит дому Верного Следа.", "Faithful serves True Trail House.", "Sadık, Sadık İz Hanesi'ne hizmet eder.", "Treu dient dem Haus Treue Spur.");
			default:
				return LocalText("Связан с кругом бойцов Mahjong Battle.", "Bound to the Mahjong Battle circle.", "Mahjong Battle çemberine bağlıdır.", "An den Mahjong Battle Kreis gebunden.");
			}
		}
		}
	}

	private string BuildCardFooterText(BattleCharacterDatabase.BattleCharacterData data, bool unlocked)
	{
		if (!string.IsNullOrEmpty(transientStatusMessage))
		{
			return transientStatusMessage;
		}
		if (data == null)
		{
			return string.Empty;
		}
		if (!data.IsEnabled)
		{
			return T("battle.character.disabled", "Disabled");
		}
		if (unlocked)
		{
			return string.Empty;
		}
		int num = ResolveUnlockPrice(data);
		if (num <= 0)
		{
			return "<b><color=#" + ColorUtility.ToHtmlStringRGB(statsPriceColor) + ">" + T("battle.character.free", "Free") + "</color></b>";
		}
		string text = ColorUtility.ToHtmlStringRGB(statsPriceColor);
		return "<b><color=#" + text + ">" + FormatPrice(num) + " " + GetCurrencyName(ResolveUnlockCurrency(data)) + "</color></b>";
	}

	public string GetPickerButtonText()
	{
		if (IsInvalidForCallbacks())
		{
			return T("battle.character.select_character", "Select Character");
		}
		BattleCharacterDatabase.BattleCharacterData characterData = GetCharacterData();
		if (characterData == null)
		{
			return T("battle.character.select_character", "Select Character");
		}
		if (!characterData.IsEnabled)
		{
			return T("battle.character.disabled", "Disabled");
		}
		if (IsCharacterUnlocked(characterData))
		{
			if (!IsCharacterSelected())
			{
				return T("battle.character.select", "Select");
			}
			return T("common.continue", "Continue");
		}
		int num = ResolveUnlockPrice(characterData);
		if (num <= 0)
		{
			return T("battle.character.buy_free", "Buy Free");
		}
		return T("battle.character.buy", "Buy");
	}

	public bool CanUsePickerButton()
	{
		if (IsInvalidForCallbacks())
		{
			return false;
		}
		return GetCharacterData()?.IsEnabled ?? false;
	}

	private static int ResolveUnlockPrice(BattleCharacterDatabase.BattleCharacterData data)
	{
		if (data == null)
		{
			return 0;
		}
		if (!BattleCharacterSelectionService.HasInstance)
		{
			return data.PriceAmount;
		}
		return BattleCharacterSelectionService.Instance.GetUnlockPrice(data.Id);
	}

	private string BuildPreviewStatusText(BattleCharacterDatabase.BattleCharacterData data, bool unlocked)
	{
		if (!string.IsNullOrEmpty(transientStatusMessage))
		{
			return transientStatusMessage;
		}
		return BuildPriceText(data, unlocked);
	}

	private void ShowPurchaseError(int price)
	{
		string text = ((price > 0) ? ("\n" + GameLocalization.Format("battle.character.need_gold", FormatPrice(price), GetCurrencyName(ResolveUnlockCurrency(GetCharacterData())))) : string.Empty);
		transientStatusMessage = T("battle.character.not_enough_gold", "Not enough currency") + text;
		transientStatusUntil = Time.unscaledTime + Mathf.Max(0.5f, purchaseErrorMessageSeconds);
		ShowPurchaseToast(transientStatusMessage);
		UpdatePreviewWindow();
		Refresh();
	}

	private void ShowPurchaseToast(string message)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			return;
		}
		EnsurePurchaseToast();
		if (!(purchaseToastObject == null) && !(purchaseToastText == null) && !(purchaseToastGroup == null))
		{
			purchaseToastText.text = message;
			purchaseToastGroup.alpha = 1f;
			purchaseToastObject.SetActive(value: true);
			if (purchaseToastRunner != null && purchaseToastRoutine != null)
			{
				purchaseToastRunner.StopCoroutine(purchaseToastRoutine);
			}
			purchaseToastRunner = this;
			purchaseToastRoutine = StartCoroutine(HidePurchaseToastAfterDelay(Mathf.Max(0.5f, purchaseErrorMessageSeconds), Mathf.Max(0.05f, purchaseErrorFadeSeconds)));
		}
	}

	private IEnumerator HidePurchaseToastAfterDelay(float delay, float fadeSeconds)
	{
		yield return new WaitForSecondsRealtime(delay);
		float elapsed = 0f;
		while (elapsed < fadeSeconds)
		{
			elapsed += Time.unscaledDeltaTime;
			if (purchaseToastGroup != null)
			{
				purchaseToastGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / fadeSeconds));
			}
			yield return null;
		}
		if (purchaseToastGroup != null)
		{
			purchaseToastGroup.alpha = 0f;
		}
		if (purchaseToastObject != null)
		{
			purchaseToastObject.SetActive(value: false);
		}
		purchaseToastRoutine = null;
		purchaseToastRunner = null;
	}

	private void EnsurePurchaseToast()
	{
		if (purchaseToastObject != null)
		{
			return;
		}
		Canvas canvas = GetComponentInParent<Canvas>();
		if (canvas == null)
		{
			canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Exclude);
		}
		if (!(canvas == null))
		{
			purchaseToastObject = new GameObject("BattleCharacterPurchaseToast", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
			purchaseToastObject.transform.SetParent(canvas.transform, worldPositionStays: false);
			RectTransform rectTransform = purchaseToastObject.transform as RectTransform;
			if (rectTransform != null)
			{
				rectTransform.anchorMin = new Vector2(0.5f, 0f);
				rectTransform.anchorMax = new Vector2(0.5f, 0f);
				rectTransform.pivot = new Vector2(0.5f, 0f);
				rectTransform.anchoredPosition = new Vector2(0f, 96f);
				rectTransform.sizeDelta = new Vector2(620f, 92f);
			}
			Image component = purchaseToastObject.GetComponent<Image>();
			component.color = new Color(0.08f, 0.07f, 0.06f, 0.92f);
			component.raycastTarget = false;
			purchaseToastGroup = purchaseToastObject.GetComponent<CanvasGroup>();
			purchaseToastGroup.blocksRaycasts = false;
			purchaseToastGroup.interactable = false;
			GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
			obj.transform.SetParent(purchaseToastObject.transform, worldPositionStays: false);
			RectTransform rectTransform2 = obj.transform as RectTransform;
			if (rectTransform2 != null)
			{
				rectTransform2.anchorMin = Vector2.zero;
				rectTransform2.anchorMax = Vector2.one;
				rectTransform2.offsetMin = new Vector2(24f, 10f);
				rectTransform2.offsetMax = new Vector2(-24f, -10f);
			}
			purchaseToastText = obj.GetComponent<TMP_Text>();
			purchaseToastText.alignment = TextAlignmentOptions.Center;
			purchaseToastText.enableAutoSizing = true;
			purchaseToastText.fontSize = 24f;
			purchaseToastText.fontSizeMin = 16f;
			purchaseToastText.fontSizeMax = 28f;
			purchaseToastText.textWrappingMode = TextWrappingModes.Normal;
			purchaseToastText.color = new Color(1f, 0.86f, 0.38f, 1f);
			purchaseToastText.raycastTarget = false;
			ApplyCharacterNameFont(purchaseToastText);
			purchaseToastObject.SetActive(value: false);
		}
	}

	private void ShowPurchaseConfirm(BattleCharacterDatabase.BattleCharacterData data, int price)
	{
		if (data == null)
		{
			return;
		}
		EnsurePurchaseConfirm();
		if (purchaseConfirmObject == null || purchaseConfirmText == null || purchaseConfirmYesButton == null || purchaseConfirmNoButton == null)
		{
			return;
		}
		pendingPurchaseButton = this;
		string priceText = price > 0
			? "\n" + FormatPrice(price) + " " + GetCurrencyName(ResolveUnlockCurrency(data))
			: string.Empty;
		purchaseConfirmText.text = LocalText("Хотите купить?", "Buy this character?", "Satın almak istiyor musunuz?", "Moechtest du kaufen?") + priceText;
		purchaseConfirmYesButton.Clicked = delegate
		{
			if (pendingPurchaseButton != null)
			{
				pendingPurchaseButton.ConfirmPendingPurchase();
			}
		};
		purchaseConfirmNoButton.Clicked = HidePurchaseConfirm;
		purchaseConfirmObject.SetActive(value: true);
		BringPurchaseConfirmToFront();
	}

	private static void HidePurchaseConfirm()
	{
		if (purchaseConfirmObject != null)
		{
			purchaseConfirmObject.SetActive(value: false);
		}
		pendingPurchaseButton = null;
	}

	private void EnsurePurchaseConfirm()
	{
		if (purchaseConfirmObject != null)
		{
			BringPurchaseConfirmToFront();
			return;
		}
		Canvas canvas = GetComponentInParent<Canvas>();
		if (canvas != null && canvas.rootCanvas != null)
		{
			canvas = canvas.rootCanvas;
		}
		if (canvas == null)
		{
			canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Exclude);
			if (canvas != null && canvas.rootCanvas != null)
			{
				canvas = canvas.rootCanvas;
			}
		}
		if (canvas == null)
		{
			return;
		}
		purchaseConfirmObject = new GameObject("BattleCharacterPurchaseConfirm", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(Image));
		purchaseConfirmObject.transform.SetParent(canvas.transform, worldPositionStays: false);
		ConfigurePurchaseConfirmCanvas(purchaseConfirmObject);
		RectTransform rootRect = purchaseConfirmObject.transform as RectTransform;
		if (rootRect != null)
		{
			rootRect.anchorMin = new Vector2(0.5f, 0.5f);
			rootRect.anchorMax = new Vector2(0.5f, 0.5f);
			rootRect.pivot = new Vector2(0.5f, 0.5f);
			rootRect.anchoredPosition = Vector2.zero;
			rootRect.sizeDelta = new Vector2(920f, 430f);
		}
		Image background = purchaseConfirmObject.GetComponent<Image>();
		BattlePopupStyle.ApplyWindow(background, raycastTarget: true);
		CanvasGroup group = purchaseConfirmObject.GetComponent<CanvasGroup>();
		group.blocksRaycasts = true;
		group.interactable = true;
		GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
		textObject.transform.SetParent(purchaseConfirmObject.transform, worldPositionStays: false);
		RectTransform textRect = textObject.transform as RectTransform;
		if (textRect != null)
		{
			textRect.anchorMin = new Vector2(0f, 0.38f);
			textRect.anchorMax = new Vector2(1f, 1f);
			textRect.offsetMin = new Vector2(72f, 0f);
			textRect.offsetMax = new Vector2(-72f, -46f);
		}
		purchaseConfirmText = textObject.GetComponent<TMP_Text>();
		purchaseConfirmText.alignment = TextAlignmentOptions.Center;
		purchaseConfirmText.enableAutoSizing = true;
		purchaseConfirmText.fontSize = 52f;
		purchaseConfirmText.fontSizeMin = 34f;
		purchaseConfirmText.fontSizeMax = 62f;
		purchaseConfirmText.textWrappingMode = TextWrappingModes.Normal;
		purchaseConfirmText.color = new Color(1f, 0.86f, 0.38f, 1f);
		purchaseConfirmText.raycastTarget = false;
		ApplyCharacterNameFont(purchaseConfirmText);
		purchaseConfirmYesButton = CreatePurchaseConfirmButton(purchaseConfirmObject.transform, "YesButton", LocalText("Да", "Yes", "Evet", "Ja"), new Vector2(-210f, -138f));
		purchaseConfirmNoButton = CreatePurchaseConfirmButton(purchaseConfirmObject.transform, "NoButton", LocalText("Нет", "No", "Hayır", "Nein"), new Vector2(210f, -138f));
		purchaseConfirmObject.SetActive(value: false);
	}

	private void BringPurchaseConfirmToFront()
	{
		if (purchaseConfirmObject == null)
		{
			return;
		}
		Canvas parentCanvas = GetComponentInParent<Canvas>();
		if (parentCanvas != null && parentCanvas.rootCanvas != null && purchaseConfirmObject.transform.parent != parentCanvas.rootCanvas.transform)
		{
			purchaseConfirmObject.transform.SetParent(parentCanvas.rootCanvas.transform, worldPositionStays: false);
		}
		ConfigurePurchaseConfirmCanvas(purchaseConfirmObject);
		purchaseConfirmObject.transform.SetAsLastSibling();
	}

	private static void ConfigurePurchaseConfirmCanvas(GameObject confirmObject)
	{
		if (confirmObject == null)
		{
			return;
		}
		Canvas overlayCanvas = confirmObject.GetComponent<Canvas>();
		if (overlayCanvas == null)
		{
			overlayCanvas = confirmObject.AddComponent<Canvas>();
		}
		overlayCanvas.overrideSorting = true;
		overlayCanvas.sortingOrder = 32700;
		if (confirmObject.GetComponent<GraphicRaycaster>() == null)
		{
			confirmObject.AddComponent<GraphicRaycaster>();
		}
	}

	private static PurchaseConfirmClickArea CreatePurchaseConfirmButton(Transform parent, string name, string label, Vector2 position)
	{
		GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(PurchaseConfirmClickArea));
		buttonObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform rect = buttonObject.transform as RectTransform;
		if (rect != null)
		{
			rect.anchorMin = new Vector2(0.5f, 0.5f);
			rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.anchoredPosition = position;
			rect.sizeDelta = new Vector2(300f, 94f);
		}
		PurchaseConfirmClickArea result = buttonObject.GetComponent<PurchaseConfirmClickArea>();
		Image buttonImage = buttonObject.GetComponent<Image>();
		BattlePopupStyle.ApplyWindow(buttonImage, raycastTarget: true);
		buttonImage.color = new Color(0.22f, 0.14f, 0.04f, 0.98f);
		GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
		labelObject.transform.SetParent(buttonObject.transform, worldPositionStays: false);
		RectTransform labelRect = labelObject.transform as RectTransform;
		if (labelRect != null)
		{
			labelRect.anchorMin = Vector2.zero;
			labelRect.anchorMax = Vector2.one;
			labelRect.offsetMin = new Vector2(22f, 8f);
			labelRect.offsetMax = new Vector2(-22f, -8f);
		}
		TMP_Text text = labelObject.GetComponent<TMP_Text>();
		text.text = label;
		text.alignment = TextAlignmentOptions.Center;
		text.enableAutoSizing = true;
		text.fontSize = 42f;
		text.fontSizeMin = 28f;
		text.fontSizeMax = 48f;
		text.textWrappingMode = TextWrappingModes.NoWrap;
		text.raycastTarget = false;
		BattlePopupStyle.ApplyText(text, silver: true);
		return result;
	}

	private static string FormatPrice(int price)
	{
		return CompactNumberFormatter.FormatCurrency(Mathf.Max(0, price));
	}

	private string FormatPriceRich(int price, BattleCharacterDatabase.CharacterPriceCurrencyType currency)
	{
		string text = ColorUtility.ToHtmlStringRGB(statsPriceColor);
		return "<b><color=#" + text + ">" + FormatPrice(price) + " " + GetCurrencyName(currency) + "</color></b>";
	}

	private static BattleCharacterDatabase.CharacterPriceCurrencyType ResolveUnlockCurrency(BattleCharacterDatabase.BattleCharacterData data)
	{
		if (data == null)
		{
			return BattleCharacterDatabase.CharacterPriceCurrencyType.OzTile;
		}
		return IsDonationCharacter(data)
			? BattleCharacterDatabase.CharacterPriceCurrencyType.OzAmetist
			: BattleCharacterDatabase.CharacterPriceCurrencyType.OzTile;
	}

	private static bool IsDonationCharacter(BattleCharacterDatabase.BattleCharacterData data)
	{
		return data != null &&
			(data.AnimalType == BattleCharacterDatabase.CharacterAnimalType.Dragon ||
			 data.UnlockType == BattleCharacterDatabase.CharacterUnlockType.PremiumCurrency ||
			 data.PriceCurrency == BattleCharacterDatabase.CharacterPriceCurrencyType.OzAmetist);
	}

	private static string GetCurrencyName(BattleCharacterDatabase.CharacterPriceCurrencyType currency)
	{
		return currency switch
		{
			BattleCharacterDatabase.CharacterPriceCurrencyType.OzAltin => GetGoldName(), 
			BattleCharacterDatabase.CharacterPriceCurrencyType.OzAmetist => "Oz Ametist", 
			BattleCharacterDatabase.CharacterPriceCurrencyType.OzTile => "OzTile", 
			_ => string.Empty, 
		};
	}

	private static string GetGoldName()
	{
		string text = GameLocalization.Text("common.oz_altin");
		if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, "common.oz_altin", StringComparison.Ordinal))
		{
			return text;
		}
		return "Oz Altın";
	}

	private static string T(string key, string fallback)
	{
		string text = GameLocalization.Text(key);
		if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, key, StringComparison.Ordinal))
		{
			return text;
		}
		return fallback;
	}

	private static string LocalText(string russian, string english, string turkish, string german)
	{
		return ((AppSettings.I != null) ? AppSettings.I.Language : GameLanguage.Turkish) switch
		{
			GameLanguage.English => english, 
			GameLanguage.Turkish => turkish, 
			GameLanguage.German => german, 
			_ => russian, 
		};
	}

	private void RefreshAllButtonsInScene()
	{
		if (IsInvalidForCallbacks())
		{
			return;
		}
		BattleCharacterButton[] array = UnityEngine.Object.FindObjectsByType<BattleCharacterButton>(FindObjectsInactive.Exclude);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] != null && !array[i].IsInvalidForCallbacks())
			{
				array[i].Refresh();
			}
		}
	}

	private TMP_Text FindChildTMPByName(string objectName)
	{
		TMP_Text[] componentsInChildren = GetComponentsInChildren<TMP_Text>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i] != null && string.Equals(componentsInChildren[i].name, objectName, StringComparison.OrdinalIgnoreCase))
			{
				return componentsInChildren[i];
			}
		}
		return null;
	}

	private TMP_Text FindStatsPanelTMP()
	{
		Transform[] componentsInChildren = GetComponentsInChildren<Transform>(includeInactive: true);
		foreach (Transform transform in componentsInChildren)
		{
			if (!(transform == null) && string.Equals(transform.name, "StatsPanel", StringComparison.OrdinalIgnoreCase))
			{
				return transform.GetComponentInChildren<TMP_Text>(includeInactive: true);
			}
		}
		return null;
	}

	private TMP_Text FindFirstStatsTMP()
	{
		TMP_Text[] componentsInChildren = GetComponentsInChildren<TMP_Text>(includeInactive: true);
		foreach (TMP_Text tMP_Text in componentsInChildren)
		{
			if (!(tMP_Text == null) && !(tMP_Text == nameText) && !string.Equals(tMP_Text.name, "NameText", StringComparison.OrdinalIgnoreCase))
			{
				return tMP_Text;
			}
		}
		return null;
	}

	private Image FindChildImageByName(string objectName)
	{
		Image[] componentsInChildren = GetComponentsInChildren<Image>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i] != null && string.Equals(componentsInChildren[i].name, objectName, StringComparison.OrdinalIgnoreCase))
			{
				return componentsInChildren[i];
			}
		}
		return null;
	}

	private static TMP_Text FindSceneTMPByName(string objectName)
	{
		if (string.IsNullOrWhiteSpace(objectName))
		{
			return null;
		}
		TMP_Text[] array = Resources.FindObjectsOfTypeAll<TMP_Text>();
		foreach (TMP_Text tMP_Text in array)
		{
			if (!(tMP_Text == null) && tMP_Text.gameObject.scene.rootCount != 0 && string.Equals(tMP_Text.name, objectName, StringComparison.OrdinalIgnoreCase))
			{
				return tMP_Text;
			}
		}
		foreach (TMP_Text tMP_Text2 in array)
		{
			if (!(tMP_Text2 == null) && tMP_Text2.gameObject.scene.rootCount != 0 && tMP_Text2.name.IndexOf(objectName, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return tMP_Text2;
			}
		}
		return null;
	}

	private static Image FindSceneImageByName(string objectName)
	{
		if (string.IsNullOrWhiteSpace(objectName))
		{
			return null;
		}
		Image[] array = Resources.FindObjectsOfTypeAll<Image>();
		foreach (Image image in array)
		{
			if (!(image == null) && image.gameObject.scene.rootCount != 0 && string.Equals(image.name, objectName, StringComparison.OrdinalIgnoreCase))
			{
				return image;
			}
		}
		foreach (Image image2 in array)
		{
			if (!(image2 == null) && image2.gameObject.scene.rootCount != 0 && image2.name.IndexOf(objectName, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return image2;
			}
		}
		return null;
	}
}

internal sealed class PurchaseConfirmClickArea : MonoBehaviour, IPointerClickHandler
{
	[NonSerialized]
	public Action Clicked;

	public void OnPointerClick(PointerEventData eventData)
	{
		Clicked?.Invoke();
	}
}
}
