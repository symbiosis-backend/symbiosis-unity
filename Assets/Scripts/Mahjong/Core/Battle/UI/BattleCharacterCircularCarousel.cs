using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MahjongGame
{

[DisallowMultipleComponent]
public sealed class BattleCharacterCircularCarousel : MonoBehaviour, IBeginDragHandler, IEventSystemHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
	[Header("Links")]
	[SerializeField]
	private RectTransform viewport;

	[SerializeField]
	private RectTransform buttonsRoot;

	[Header("Buttons")]
	[SerializeField]
	private bool autoCollectButtons = true;

	[SerializeField]
	private List<BattleCharacterButton> buttons = new List<BattleCharacterButton>();

	[Header("Layout")]
	[SerializeField]
	private bool enforceLandscapeProLayout = true;

	[SerializeField]
	private float landscapeProSpacing = 860f;

	[SerializeField]
	private float landscapeProStageY = -78f;

	[SerializeField]
	private Vector2 landscapeProStageSize = new Vector2(2360f, 760f);

	[SerializeField]
	private Color landscapeProStageColor = new Color(0.015f, 0.018f, 0.022f, 0f);

	[SerializeField]
	private bool useRightVerticalCharacterRail = true;

	[SerializeField]
	private Vector2 mainCharacterFramePosition = new Vector2(-380f, 190f);

	[SerializeField]
	private Vector2 mainCharacterFrameSize = new Vector2(1640f, 930f);

	[SerializeField]
	private Vector2 rightRailFramePosition = new Vector2(790f, 25f);

	[SerializeField]
	private Vector2 rightRailCardSize = new Vector2(510f, 350f);

	[SerializeField]
	private float rightRailCardGap = 48f;

	[SerializeField]
	private float spacing = 760f;

	[SerializeField]
	private int visibleSideCount = 1;

	[Header("Swipe")]
	[SerializeField]
	private float swipeThresholdPixels = 40f;

	[SerializeField]
	private float dragPreviewFactor = 0.18f;

	[SerializeField]
	private bool allowPreviewWhileDragging = true;

	[Header("Snap")]
	[SerializeField]
	private float snapSpeed = 10f;

	[SerializeField]
	private float snapFinishThreshold = 0.001f;

	[Header("Auto Scroll")]
	[SerializeField]
	private bool autoScroll;

	[SerializeField]
	private float autoScrollDelay = 2f;

	[SerializeField]
	private float autoScrollStepDelay = 2.5f;

	[SerializeField]
	private bool autoScrollToLeft = true;

	[Header("Visual")]
	[SerializeField]
	private bool animateScale = true;

	[SerializeField]
	private float centerScale = 1f;

	[SerializeField]
	private float sideScale = 0.84f;

	[SerializeField]
	private float scaleLerpSpeed = 8f;

	[Header("Alpha")]
	[SerializeField]
	private bool animateAlpha = true;

	[SerializeField]
	private float centerAlpha = 1f;

	[SerializeField]
	private float sideAlpha = 0.82f;

	[SerializeField]
	private float hiddenAlpha;

	[SerializeField]
	private float alphaLerpSpeed = 8f;

	[Header("Selection")]
	[SerializeField]
	private bool autoSelectCenteredCharacter = true;

	[SerializeField]
	private float selectDelayAfterSnap = 0.1f;

	private readonly Dictionary<BattleCharacterButton, CanvasGroup> canvasGroups = new Dictionary<BattleCharacterButton, CanvasGroup>();

	private float currentVirtualIndex;

	private float targetVirtualIndex;

	private float visualVirtualIndex;

	private bool isDragging;

	private bool isSnapping;

	private float dragStartX;

	private float dragCurrentOffsetPixels;

	private bool isRailDragging;

	private float railDragStartY;

	private float railScrollStartOffset;

	private float railScrollOffset;

	private int rightRailCharacterCount;

	private string previewCharacterId = string.Empty;

	private float idleTimer;

	private float autoStepTimer;

	private float snappedTime;

	private BattleCharacterButton centeredButton;

	private string lastSelectedCenteredId = string.Empty;

	private bool loggedEmptyButtons;

	private bool hasPreviewedFirstHero;

	private Image stageBackdrop;

	private Image stageRail;

	private Button previousCharacterButton;

	private Button nextCharacterButton;

	private RectTransform rightRailViewport;

	private RectMask2D rightRailMask;

	private readonly List<RailProfileCard> rightRailCards = new List<RailProfileCard>();

	private Sprite cachedRailAvatarFrameSprite;

	private Sprite cachedRailCardSprite;

	private Sprite cachedRailPhotoSprite;

	private Sprite cachedRailStateSprite;

	private TMP_Text railScrollHintText;

	private Button railScrollUpButton;

	private Button railScrollDownButton;

	private Image fullscreenBackground;

	private RectTransform firstHeroDialogueRoot;

	private TMP_Text blackYangDialogueText;

	private TMP_Text whiteYinDialogueText;

	private RectTransform firstHeroDialogueArrow;

	private Vector2 firstHeroDialogueArrowBasePosition;

	private Sprite cachedBattleLobbyAvatarFrameSprite;

	private Sprite cachedBlackYangAvatarSprite;

	private Sprite cachedWhiteYinAvatarSprite;

	private Sprite cachedFirstHeroPointerSprite;

	private BattleCharacterSelectionService subscribedSelectionService;

	public BattleCharacterButton CenteredButton => centeredButton;

	public BattleCharacterButton ActivePreviewButton => FindButtonById(previewCharacterId) ?? centeredButton ?? FindButtonForCurrentSelection() ?? FindTemplateButton();

	private void Reset()
	{
		if (viewport == null)
		{
			viewport = base.transform as RectTransform;
		}
		if (buttonsRoot == null && base.transform.childCount > 0)
		{
			buttonsRoot = base.transform.GetChild(0) as RectTransform;
		}
	}

	private void Awake()
	{
		DisableAutoScroll();
		if (viewport == null)
		{
			viewport = base.transform as RectTransform;
		}
		if (buttonsRoot == null)
		{
			buttonsRoot = base.transform as RectTransform;
		}
		CollectButtonsIfNeeded();
		EnsureButtonsForCatalog();
		BindButtons();
		EnsureCanvasGroups();
		currentVirtualIndex = 0f;
		targetVirtualIndex = 0f;
		visualVirtualIndex = 0f;
	}

	private void OnEnable()
	{
		DisableAutoScroll();
		hasPreviewedFirstHero = false;
		base.transform.SetAsLastSibling();
		ApplyLandscapeProLayoutIfNeeded();
		EnsureFirstHeroDialogue();
		CollectButtonsIfNeeded();
		EnsureButtonsForCatalog();
		BindButtons();
		EnsureCanvasGroups();
		EnsureNavigationButtons();
		TryBindSelectionService();
		BattleCharacterDatabase.CatalogChanged += OnCharacterCatalogChanged;
		AppSettings.OnLanguageChanged -= OnFirstHeroDialogueLanguageChanged;
		AppSettings.OnLanguageChanged += OnFirstHeroDialogueLanguageChanged;
		SnapToSelectedOrFirst(instant: true);
		RefreshButtons();
		RefreshFirstHeroDialogue();
		StartCoroutine(RefreshNextFrame());
	}

	private void DisableAutoScroll()
	{
		autoScroll = false;
		idleTimer = 0f;
		autoStepTimer = 0f;
	}

	private void OnDisable()
	{
		UnbindSelectionService();
		BattleCharacterDatabase.CatalogChanged -= OnCharacterCatalogChanged;
		AppSettings.OnLanguageChanged -= OnFirstHeroDialogueLanguageChanged;
		BattleLobbyUI battleLobbyUI = UnityEngine.Object.FindAnyObjectByType<BattleLobbyUI>(FindObjectsInactive.Include);
		if (battleLobbyUI != null && battleLobbyUI.isActiveAndEnabled)
		{
			battleLobbyUI.RequestRestoreLobbyHudAfterCharacterCarouselClosed();
		}
	}

	private void Update()
	{
		if (buttons.Count == 0)
		{
			return;
		}
		float unscaledDeltaTime = Time.unscaledDeltaTime;
		if (!isDragging)
		{
			idleTimer += unscaledDeltaTime;
		}
		else
		{
			idleTimer = 0f;
		}
		if (!isDragging && !isSnapping && autoScroll && idleTimer >= autoScrollDelay)
		{
			autoStepTimer += unscaledDeltaTime;
			if (autoStepTimer >= autoScrollStepDelay)
			{
				autoStepTimer = 0f;
				Step(autoScrollToLeft ? 1 : (-1));
			}
		}
		else if (isDragging || isSnapping || idleTimer < autoScrollDelay)
		{
			autoStepTimer = 0f;
		}
		if (isSnapping)
		{
			visualVirtualIndex = Mathf.Lerp(visualVirtualIndex, targetVirtualIndex, snapSpeed * unscaledDeltaTime);
			if (Mathf.Abs(visualVirtualIndex - targetVirtualIndex) <= snapFinishThreshold)
			{
				visualVirtualIndex = targetVirtualIndex;
				currentVirtualIndex = targetVirtualIndex;
				isSnapping = false;
				snappedTime = Time.unscaledTime;
			}
		}
		else if (allowPreviewWhileDragging && isDragging)
		{
			float num = dragCurrentOffsetPixels / Mathf.Max(1f, spacing) * dragPreviewFactor;
			visualVirtualIndex = currentVirtualIndex - num;
		}
		else
		{
			visualVirtualIndex = Mathf.Lerp(visualVirtualIndex, currentVirtualIndex, snapSpeed * unscaledDeltaTime);
		}
		UpdateButtonPositions();
		if (useRightVerticalCharacterRail)
		{
			centeredButton = FindButtonForCurrentSelection();
		}
		else
		{
			UpdateCenteredButton();
		}
		UpdateVisualStates();
		UpdateFirstHeroDialogueArrow();
		if (!useRightVerticalCharacterRail)
			TryAutoSelectCentered();
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (useRightVerticalCharacterRail)
		{
			isRailDragging = false;
			dragCurrentOffsetPixels = 0f;
			return;
		}
		if (buttons.Count != 0)
		{
			isDragging = true;
			isSnapping = false;
			dragStartX = eventData.position.x;
			dragCurrentOffsetPixels = 0f;
			idleTimer = 0f;
			autoStepTimer = 0f;
		}
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (useRightVerticalCharacterRail)
		{
			dragCurrentOffsetPixels = 0f;
			return;
		}
		if (isDragging)
		{
			dragCurrentOffsetPixels = eventData.position.x - dragStartX;
		}
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (useRightVerticalCharacterRail)
		{
			isRailDragging = false;
			railScrollOffset = SnapRailScrollOffset(railScrollOffset);
			dragCurrentOffsetPixels = 0f;
			return;
		}
		if (!isDragging)
		{
			return;
		}
		isDragging = false;
		float num = dragCurrentOffsetPixels;
		if (Mathf.Abs(num) >= swipeThresholdPixels)
		{
			if (num < 0f)
			{
				Step(1);
			}
			else
			{
				Step(-1);
			}
		}
		else
		{
			SnapToCurrent();
		}
		dragCurrentOffsetPixels = 0f;
		idleTimer = 0f;
		autoStepTimer = 0f;
	}

	public void OnScroll(PointerEventData eventData)
	{
		if (!useRightVerticalCharacterRail)
		{
			return;
		}
		float railStep = GetRailPageStep();
		float direction = Mathf.Sign(eventData.scrollDelta.y);
		if (Mathf.Abs(direction) > 0.01f)
		{
			railScrollOffset = SnapRailScrollOffset(railScrollOffset - direction * railStep);
		}
	}

	public void FocusButton(BattleCharacterButton button, bool selectCharacter)
	{
		if (button == null || buttons.Count == 0)
		{
			return;
		}
		int num = buttons.IndexOf(button);
		if (num >= 0)
		{
			float num2 = WrapDelta((float)num - currentVirtualIndex, buttons.Count);
			currentVirtualIndex += num2;
			targetVirtualIndex = currentVirtualIndex;
			isDragging = false;
			isSnapping = true;
			idleTimer = 0f;
			autoStepTimer = 0f;
			if (selectCharacter)
			{
				button.SelectDirectly();
			}
		}
	}

	public void RefreshButtons()
	{
		CollectButtonsIfNeeded();
		EnsureButtonsForCatalog();
		BindButtons();
		EnsureCanvasGroups();
		EnsureNavigationButtons();
		for (int i = 0; i < buttons.Count; i++)
		{
			if (buttons[i] != null)
			{
				buttons[i].Refresh();
			}
		}
		UpdateButtonPositions();
		UpdateCenteredButton();
		UpdateVisualStates();
		RefreshFirstHeroDialogue();
	}

	public void SnapToSelectedOrFirst(bool instant)
	{
		BattleCharacterButton battleCharacterButton = FindButtonForCurrentSelection();
		if (battleCharacterButton == null && buttons.Count > 0)
		{
			battleCharacterButton = buttons[0];
		}
		if (battleCharacterButton == null)
		{
			return;
		}
		int num = buttons.IndexOf(battleCharacterButton);
		if (num >= 0)
		{
			float num2 = WrapDelta((float)num - currentVirtualIndex, buttons.Count);
			if (instant)
			{
				currentVirtualIndex += num2;
				targetVirtualIndex = currentVirtualIndex;
				visualVirtualIndex = currentVirtualIndex;
				isSnapping = false;
			}
			else
			{
				currentVirtualIndex += num2;
				targetVirtualIndex = currentVirtualIndex;
				isSnapping = true;
			}
			UpdateButtonPositions();
			UpdateCenteredButton();
			UpdateVisualStates();
		}
	}

	private void Step(int direction)
	{
		if (buttons.Count != 0)
		{
			currentVirtualIndex += direction;
			targetVirtualIndex = currentVirtualIndex;
			isSnapping = true;
		}
	}

	private void SnapToCurrent()
	{
		targetVirtualIndex = currentVirtualIndex;
		isSnapping = true;
	}

	private void CollectButtonsIfNeeded()
	{
		if (!autoCollectButtons || buttonsRoot == null)
		{
			return;
		}
		buttons.Clear();
		BattleCharacterButton[] array = buttonsRoot.GetComponentsInChildren<BattleCharacterButton>(includeInactive: true);
		if (array == null || array.Length == 0)
		{
			array = UnityEngine.Object.FindObjectsByType<BattleCharacterButton>(FindObjectsInactive.Include);
		}
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] != null && !buttons.Contains(array[i]))
			{
				buttons.Add(array[i]);
			}
		}
		if (buttons.Count == 0 && !loggedEmptyButtons)
		{
			loggedEmptyButtons = true;
			Debug.LogWarning("[BattleCharacterCircularCarousel] No BattleCharacterButton objects found for carousel.", this);
		}
	}

	private void EnsureButtonsForCatalog()
	{
		if (buttonsRoot == null)
		{
			return;
		}
		BattleCharacterDatabase battleCharacterDatabase = (BattleCharacterDatabase.HasInstance ? BattleCharacterDatabase.Instance : UnityEngine.Object.FindAnyObjectByType<BattleCharacterDatabase>(FindObjectsInactive.Include));
		if (battleCharacterDatabase == null)
		{
			return;
		}
		IReadOnlyList<BattleCharacterDatabase.BattleCharacterData> characters = battleCharacterDatabase.Characters;
		if (characters == null || characters.Count == 0)
		{
			return;
		}
		BattleCharacterButton battleCharacterButton = FindTemplateButton();
		if (battleCharacterButton == null)
		{
			return;
		}
		for (int i = 0; i < characters.Count; i++)
		{
			BattleCharacterDatabase.BattleCharacterData battleCharacterData = characters[i];
			if (battleCharacterData != null && battleCharacterData.IsEnabled && !string.IsNullOrWhiteSpace(battleCharacterData.Id) && !(FindButtonById(battleCharacterData.Id) != null))
			{
				BattleCharacterButton battleCharacterButton2 = UnityEngine.Object.Instantiate(battleCharacterButton, buttonsRoot);
				battleCharacterButton2.name = battleCharacterData.Id.Replace("_", string.Empty);
				battleCharacterButton2.SetCharacterId(battleCharacterData.Id, refresh: false);
				RectTransform rectTransform = battleCharacterButton2.RectTransform;
				if (rectTransform != null)
				{
					rectTransform.anchoredPosition = Vector2.zero;
				}
				if (!buttons.Contains(battleCharacterButton2))
				{
					buttons.Add(battleCharacterButton2);
				}
			}
		}
	}

	private BattleCharacterButton FindTemplateButton()
	{
		for (int i = 0; i < buttons.Count; i++)
		{
			if (buttons[i] != null)
			{
				return buttons[i];
			}
		}
		if (!(buttonsRoot != null))
		{
			return null;
		}
		return buttonsRoot.GetComponentInChildren<BattleCharacterButton>(includeInactive: true);
	}

	private BattleCharacterButton FindButtonById(string characterId)
	{
		if (string.IsNullOrWhiteSpace(characterId))
		{
			return null;
		}
		for (int i = 0; i < buttons.Count; i++)
		{
			BattleCharacterButton battleCharacterButton = buttons[i];
			if (!(battleCharacterButton == null) && string.Equals(battleCharacterButton.CharacterId, characterId, StringComparison.Ordinal))
			{
				return battleCharacterButton;
			}
		}
		return null;
	}

	private void BindButtons()
	{
		for (int i = 0; i < buttons.Count; i++)
		{
			if (!(buttons[i] == null))
			{
				buttons[i].SetOwnerCarousel(this);
			}
		}
	}

	private void EnsureCanvasGroups()
	{
		canvasGroups.Clear();
		for (int i = 0; i < buttons.Count; i++)
		{
			BattleCharacterButton battleCharacterButton = buttons[i];
			if (!(battleCharacterButton == null))
			{
				CanvasGroup canvasGroup = battleCharacterButton.GetComponent<CanvasGroup>();
				if (canvasGroup == null)
				{
					canvasGroup = battleCharacterButton.gameObject.AddComponent<CanvasGroup>();
				}
				canvasGroups[battleCharacterButton] = canvasGroup;
			}
		}
	}

	private void UpdateButtonPositions()
	{
		int count = buttons.Count;
		if (count == 0)
		{
			return;
		}
		if (useRightVerticalCharacterRail)
		{
			UpdateVerticalRailPositions();
			return;
		}
		for (int i = 0; i < count; i++)
		{
			BattleCharacterButton battleCharacterButton = buttons[i];
			if (!(battleCharacterButton == null) && !(battleCharacterButton.RectTransform == null))
			{
				float x = ResolveSymmetricSlot(i, count) * spacing;
				Vector2 anchoredPosition = battleCharacterButton.RectTransform.anchoredPosition;
				anchoredPosition.x = x;
				anchoredPosition.y = 0f;
				battleCharacterButton.RectTransform.anchoredPosition = anchoredPosition;
			}
		}
	}

	private void ApplyLandscapeProLayoutIfNeeded()
	{
		if (enforceLandscapeProLayout)
		{
			RectTransform rectTransform = base.transform as RectTransform;
			if (rectTransform != null)
			{
				rectTransform.anchorMin = Vector2.zero;
				rectTransform.anchorMax = Vector2.one;
				rectTransform.offsetMin = Vector2.zero;
				rectTransform.offsetMax = Vector2.zero;
				rectTransform.pivot = new Vector2(0.5f, 0.5f);
				rectTransform.localScale = Vector3.one;
			}
			if (viewport != null)
			{
				viewport.anchorMin = Vector2.zero;
				viewport.anchorMax = Vector2.one;
				viewport.offsetMin = Vector2.zero;
				viewport.offsetMax = Vector2.zero;
				viewport.pivot = new Vector2(0.5f, 0.5f);
				viewport.localScale = Vector3.one;
			}
			if (buttonsRoot != null)
			{
				float y = ((landscapeProStageY > -70f) ? (-78f) : landscapeProStageY);
				buttonsRoot.anchorMin = new Vector2(0.5f, 0.5f);
				buttonsRoot.anchorMax = new Vector2(0.5f, 0.5f);
				buttonsRoot.pivot = new Vector2(0.5f, 0.5f);
				buttonsRoot.anchoredPosition = new Vector2(0f, y);
				buttonsRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, landscapeProStageSize.x);
				buttonsRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, landscapeProStageSize.y);
				buttonsRoot.localScale = Vector3.one;
			}
			EnsureFullscreenBackground(rectTransform);
			HideLandscapeStageChrome(rectTransform);
			spacing = Mathf.Clamp(landscapeProSpacing, 830f, 900f);
			visibleSideCount = 0;
			centerScale = Mathf.Clamp(centerScale, 0.96f, 1.04f);
			sideScale = Mathf.Clamp(sideScale, 0.76f, 0.88f);
			sideAlpha = Mathf.Clamp(sideAlpha, 0.72f, 0.88f);
			EnsureNavigationButtons();
		}
	}

	private void HideLandscapeStageChrome(RectTransform root)
	{
		if (!(root == null))
		{
			stageBackdrop = FindStageImage("CharacterSelectStageBackdrop", root);
			if (stageBackdrop != null)
			{
				stageBackdrop.gameObject.SetActive(value: false);
			}
			stageRail = FindStageImage("CharacterSelectStageRail", root);
			if (stageRail != null)
			{
				stageRail.gameObject.SetActive(value: false);
			}
		}
	}

	private static Image FindStageImage(string objectName, RectTransform parent)
	{
		Transform transform = parent.Find(objectName);
		if (!(transform != null))
		{
			return null;
		}
		return transform.GetComponent<Image>();
	}

	private void EnsureNavigationButtons()
	{
		if (!(base.transform as RectTransform == null))
		{
			DisableNavigationButton("PreviousCharacterButton", ref previousCharacterButton);
			DisableNavigationButton("NextCharacterButton", ref nextCharacterButton);
		}
	}

	private void EnsureFullscreenBackground(RectTransform root)
	{
		if (root == null)
		{
			return;
		}
		if (fullscreenBackground == null)
		{
			Transform existing = root.Find("CharacterSelectFullscreenBackground");
			fullscreenBackground = (existing != null) ? existing.GetComponent<Image>() : null;
		}
		if (fullscreenBackground == null)
		{
			GameObject obj = new GameObject("CharacterSelectFullscreenBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			obj.transform.SetParent(root, worldPositionStays: false);
			fullscreenBackground = obj.GetComponent<Image>();
		}
		RectTransform rect = fullscreenBackground.rectTransform;
		if (rect != null)
		{
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.localScale = Vector3.one;
		}
		BattlePopupStyle.ApplyWindow(fullscreenBackground, raycastTarget: false);
		fullscreenBackground.color = Color.white;
		fullscreenBackground.transform.SetAsFirstSibling();
	}

	private void EnsureFirstHeroDialogue()
	{
		RectTransform carouselRoot = base.transform as RectTransform;
		if (carouselRoot == null)
		{
			return;
		}
		if (firstHeroDialogueRoot == null)
		{
			Transform existing = carouselRoot.Find("FirstHeroDialogue");
			firstHeroDialogueRoot = existing as RectTransform;
		}
		if (firstHeroDialogueRoot == null)
		{
			GameObject rootObject = new GameObject("FirstHeroDialogue", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			rootObject.transform.SetParent(carouselRoot, worldPositionStays: false);
			firstHeroDialogueRoot = rootObject.GetComponent<RectTransform>();
		}
		Image dialogueWindow = firstHeroDialogueRoot.GetComponent<Image>();
		if (dialogueWindow == null)
		{
			dialogueWindow = firstHeroDialogueRoot.gameObject.AddComponent<Image>();
		}
		if (!BattlePopupStyle.ApplyWindow(dialogueWindow, raycastTarget: false))
		{
			dialogueWindow.sprite = null;
			dialogueWindow.color = new Color(0.035f, 0.025f, 0.018f, 0.97f);
			dialogueWindow.raycastTarget = false;
		}
		ApplyRailRect(firstHeroDialogueRoot, new Vector2(-365f, 65f), new Vector2(1360f, 720f));
		EnsureFirstHeroDialogueRow(
			firstHeroDialogueRoot,
			"BlackYangRow",
			"BlackYang",
			LoadRailSprite(ref cachedBlackYangAvatarSprite, "ProfileAvatars/AvatarsMale/Avatar14"),
			new Vector2(0f, 154f),
			ref blackYangDialogueText);
		EnsureFirstHeroDialogueRow(
			firstHeroDialogueRoot,
			"WhiteYinRow",
			"WhiteYin",
			LoadRailSprite(ref cachedWhiteYinAvatarSprite, "ProfileAvatars/AvatarsFemale/AvatarFemale9"),
			new Vector2(0f, -154f),
			ref whiteYinDialogueText);
		HideFirstHeroDialogueDivider(firstHeroDialogueRoot);
		EnsureFirstHeroDialogueArrow(firstHeroDialogueRoot);
	}

	private void EnsureFirstHeroDialogueRow(
		RectTransform parent,
		string objectName,
		string speakerName,
		Sprite portraitSprite,
		Vector2 position,
		ref TMP_Text bodyText)
	{
		Transform existing = parent.Find(objectName);
		GameObject rowObject = existing != null
			? existing.gameObject
			: new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		if (existing == null)
		{
			rowObject.transform.SetParent(parent, worldPositionStays: false);
		}
		RectTransform row = rowObject.GetComponent<RectTransform>();
		ApplyRailRect(row, position, new Vector2(1160f, 274f));
		Image rowBackground = rowObject.GetComponent<Image>();
		rowBackground.enabled = false;
		rowBackground.raycastTarget = false;

		Image portrait = EnsureFirstHeroDialogueImage(row, "Portrait");
		ApplyRailRect(portrait.rectTransform, new Vector2(-450f, 0f), new Vector2(220f, 220f));
		ConfigureFirstHeroDialogueImage(portrait, portraitSprite, sliced: false, Color.white);

		Image avatarFrame = EnsureFirstHeroDialogueImage(row, "AvatarFrame");
		ApplyRailRect(avatarFrame.rectTransform, new Vector2(-450f, 0f), new Vector2(282f, 282f));
		ConfigureFirstHeroDialogueImage(
			avatarFrame,
			LoadRailSprite(ref cachedBattleLobbyAvatarFrameSprite, "Mahjong/Sprites/BattleLobbyUI/AvatarFrameGenerated"),
			sliced: false,
			Color.white);

		TMP_Text nameText = EnsureFirstHeroDialogueText(row, "SpeakerName");
		ApplyRailRect(nameText.rectTransform, new Vector2(100f, 82f), new Vector2(820f, 58f));
		ConfigureFirstHeroDialogueText(nameText, 42f, 31f, 48f, FontStyles.Bold, TextAlignmentOptions.Left);
		nameText.text = speakerName;
		nameText.color = new Color(1f, 0.79f, 0.22f, 1f);

		bodyText = EnsureFirstHeroDialogueText(row, "Body");
		ApplyRailRect(bodyText.rectTransform, new Vector2(100f, -37f), new Vector2(820f, 158f));
		ConfigureFirstHeroDialogueText(bodyText, 37f, 28f, 42f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
		bodyText.color = new Color(1f, 0.91f, 0.70f, 1f);
	}

	private static void HideFirstHeroDialogueDivider(RectTransform parent)
	{
		Transform divider = parent.Find("DialogueDivider");
		if (divider != null)
		{
			divider.gameObject.SetActive(false);
		}
	}

	private static Image EnsureFirstHeroDialogueImage(RectTransform parent, string objectName)
	{
		Transform existing = parent.Find(objectName);
		GameObject imageObject = existing != null
			? existing.gameObject
			: new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		if (existing == null)
		{
			imageObject.transform.SetParent(parent, worldPositionStays: false);
		}
		return imageObject.GetComponent<Image>();
	}

	private static TMP_Text EnsureFirstHeroDialogueText(RectTransform parent, string objectName)
	{
		Transform existing = parent.Find(objectName);
		GameObject textObject = existing != null
			? existing.gameObject
			: new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		if (existing == null)
		{
			textObject.transform.SetParent(parent, worldPositionStays: false);
		}
		return textObject.GetComponent<TMP_Text>();
	}

	private static void ConfigureFirstHeroDialogueImage(Image image, Sprite sprite, bool sliced, Color color)
	{
		if (image == null)
		{
			return;
		}
		image.sprite = sprite;
		image.enabled = sprite != null;
		image.type = sliced && sprite != null && sprite.border.sqrMagnitude > 0.01f
			? Image.Type.Sliced
			: Image.Type.Simple;
		image.preserveAspect = !sliced;
		image.color = color;
		image.raycastTarget = false;
	}

	private static void ConfigureFirstHeroDialogueText(
		TMP_Text text,
		float fontSize,
		float fontSizeMin,
		float fontSizeMax,
		FontStyles fontStyle,
		TextAlignmentOptions alignment)
	{
		if (text == null)
		{
			return;
		}
		text.fontSize = fontSize;
		text.fontSizeMin = fontSizeMin;
		text.fontSizeMax = fontSizeMax;
		text.enableAutoSizing = true;
		text.fontStyle = fontStyle;
		text.alignment = alignment;
		text.textWrappingMode = TextWrappingModes.Normal;
		text.overflowMode = TextOverflowModes.Truncate;
		text.raycastTarget = false;
		text.outlineColor = new Color(0f, 0f, 0f, 0.82f);
		text.outlineWidth = 0.14f;
		BattlePopupStyle.ApplyFontOnly(text);
	}

	private void EnsureFirstHeroDialogueArrow(RectTransform parent)
	{
		if (firstHeroDialogueArrow == null)
		{
			Transform existing = parent.Find("HeroRailPointer");
			firstHeroDialogueArrow = existing as RectTransform;
		}
		if (firstHeroDialogueArrow == null)
		{
			GameObject arrowObject = new GameObject("HeroRailPointer", typeof(RectTransform));
			arrowObject.transform.SetParent(parent, worldPositionStays: false);
			firstHeroDialogueArrow = arrowObject.GetComponent<RectTransform>();
		}
		firstHeroDialogueArrowBasePosition = new Vector2(810f, 0f);
		ApplyRailRect(firstHeroDialogueArrow, firstHeroDialogueArrowBasePosition, new Vector2(260f, 260f));
		DisableFirstHeroArrowPart(firstHeroDialogueArrow, "Shaft");
		DisableFirstHeroArrowPart(firstHeroDialogueArrow, "HeadTop");
		DisableFirstHeroArrowPart(firstHeroDialogueArrow, "HeadBottom");
		Image decorativePointer = EnsureFirstHeroDialogueImage(firstHeroDialogueArrow, "DecorativePointer");
		ApplyRailRect(decorativePointer.rectTransform, Vector2.zero, new Vector2(220f, 238f));
		decorativePointer.rectTransform.localRotation = Quaternion.identity;
		decorativePointer.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
		ConfigureFirstHeroDialogueImage(
			decorativePointer,
			LoadRailSprite(ref cachedFirstHeroPointerSprite, "Mahjong/Sprites/BambooLobby/Mahjong_Bamboo_CarouselArrow"),
			sliced: false,
			Color.white);
		firstHeroDialogueArrow.SetAsLastSibling();
	}

	private static void DisableFirstHeroArrowPart(RectTransform parent, string objectName)
	{
		Transform part = parent.Find(objectName);
		if (part != null)
		{
			part.gameObject.SetActive(false);
		}
	}

	private void RefreshFirstHeroDialogue()
	{
		TryBindSelectionService();
		EnsureFirstHeroDialogue();
		if (firstHeroDialogueRoot == null)
		{
			return;
		}
		bool shouldShow = !hasPreviewedFirstHero &&
			BattleCharacterSelectionService.HasInstance &&
			string.IsNullOrWhiteSpace(BattleCharacterSelectionService.Instance.SelectedCharacterId) &&
			HasFirstFreeCharacterChoice();
		firstHeroDialogueRoot.gameObject.SetActive(shouldShow);
		if (!shouldShow)
		{
			return;
		}
		if (blackYangDialogueText != null)
		{
			blackYangDialogueText.text = GameLocalization.Text("battle.character.first_hero.blackyang");
		}
		if (whiteYinDialogueText != null)
		{
			whiteYinDialogueText.text = GameLocalization.Text("battle.character.first_hero.whiteyin");
		}
		firstHeroDialogueRoot.SetAsLastSibling();
	}

	private void UpdateFirstHeroDialogueArrow()
	{
		if (firstHeroDialogueArrow == null || firstHeroDialogueRoot == null || !firstHeroDialogueRoot.gameObject.activeInHierarchy)
		{
			return;
		}
		float offset = Mathf.Sin(Time.unscaledTime * 2.4f) * 8f;
		firstHeroDialogueArrow.anchoredPosition = firstHeroDialogueArrowBasePosition + new Vector2(offset, 0f);
	}

	private void OnFirstHeroDialogueLanguageChanged(GameLanguage _)
	{
		RefreshButtons();
	}

	private bool HasFirstFreeCharacterChoice()
	{
		if (!BattleCharacterSelectionService.HasInstance)
		{
			return false;
		}
		BattleCharacterSelectionService selectionService = BattleCharacterSelectionService.Instance;
		for (int i = 0; i < buttons.Count; i++)
		{
			BattleCharacterButton button = buttons[i];
			if (button != null && selectionService.IsFirstFreeCharacterChoice(button.CharacterId))
			{
				return true;
			}
		}
		return false;
	}

	private void TryBindSelectionService()
	{
		BattleCharacterSelectionService current = BattleCharacterSelectionService.HasInstance
			? BattleCharacterSelectionService.Instance
			: null;
		if (subscribedSelectionService == current)
		{
			return;
		}
		UnbindSelectionService();
		subscribedSelectionService = current;
		if (subscribedSelectionService != null)
		{
			subscribedSelectionService.SelectedCharacterChanged += OnSelectedCharacterChanged;
		}
	}

	private void UnbindSelectionService()
	{
		if (subscribedSelectionService != null)
		{
			subscribedSelectionService.SelectedCharacterChanged -= OnSelectedCharacterChanged;
			subscribedSelectionService = null;
		}
	}

	private void UpdateVerticalRailPositions()
	{
		BattleCharacterButton mainButton = FindButtonById(previewCharacterId) ?? FindButtonForCurrentSelection();
		RectTransform mainParent = buttonsRoot != null ? buttonsRoot : base.transform as RectTransform;
		RectTransform railViewport = EnsureRightRailViewport();
		EnsureRailScrollHint();
		float railStep = rightRailCardSize.y + rightRailCardGap;
		float railHeight = rightRailCardSize.y * 2f + rightRailCardGap;
		railScrollOffset = isRailDragging ? WrapRailScrollOffset(railScrollOffset) : SnapRailScrollOffset(railScrollOffset);
		for (int i = 0; i < buttons.Count; i++)
		{
			BattleCharacterButton button = buttons[i];
			if (button == null || button.RectTransform == null)
			{
				continue;
			}
			if (button == mainButton)
			{
				button.RectTransform.gameObject.SetActive(value: true);
				if (mainParent != null && button.RectTransform.parent != mainParent)
				{
					button.RectTransform.SetParent(mainParent, worldPositionStays: false);
				}
				button.Refresh();
				button.SetCarouselFrame(mainCharacterFramePosition, mainCharacterFrameSize, profileCard: false);
				continue;
			}
			button.RectTransform.gameObject.SetActive(value: false);
		}
		UpdateCleanRightRailCards(mainButton, railViewport, railHeight, railStep);
	}

	private void UpdateCleanRightRailCards(BattleCharacterButton mainButton, RectTransform railViewport, float railHeight, float railStep)
	{
		if (railViewport == null)
		{
			return;
		}
		List<BattleCharacterDatabase.BattleCharacterData> railCharacters = GetRailCharacters();
		rightRailCharacterCount = railCharacters.Count;
		EnsureRailCardCount(railCharacters.Count, railViewport);
		float firstRailY = railHeight * 0.5f - rightRailCardSize.y * 0.5f;
		for (int i = 0; i < rightRailCards.Count; i++)
		{
			RailProfileCard card = rightRailCards[i];
			if (card == null || card.Root == null)
			{
				continue;
			}
			bool active = i < railCharacters.Count;
			card.Root.gameObject.SetActive(active);
			if (!active)
			{
				continue;
			}
			BattleCharacterDatabase.BattleCharacterData data = railCharacters[i];
			float y = ResolveWrappedRailCardY(i, firstRailY, railStep, railCharacters.Count);
			ApplyRailCard(card, data, new Vector2(0f, y));
		}
	}

	private List<BattleCharacterDatabase.BattleCharacterData> GetRailCharacters()
	{
		List<BattleCharacterDatabase.BattleCharacterData> result = new List<BattleCharacterDatabase.BattleCharacterData>();
		BattleCharacterDatabase database = BattleCharacterDatabase.HasInstance ? BattleCharacterDatabase.Instance : UnityEngine.Object.FindAnyObjectByType<BattleCharacterDatabase>(FindObjectsInactive.Include);
		if (database == null)
		{
			return result;
		}
		List<BattleCharacterDatabase.BattleCharacterData> enabledCharacters = database.GetEnabledCharacters();
		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
		BattleCharacterDatabase.CharacterAnimalType[] animalOrder =
		{
			BattleCharacterDatabase.CharacterAnimalType.Tiger,
			BattleCharacterDatabase.CharacterAnimalType.Fox,
			BattleCharacterDatabase.CharacterAnimalType.Wolf,
			BattleCharacterDatabase.CharacterAnimalType.Bear,
			BattleCharacterDatabase.CharacterAnimalType.Dragon,
			BattleCharacterDatabase.CharacterAnimalType.Dog
		};
		for (int animalIndex = 0; animalIndex < animalOrder.Length; animalIndex++)
		{
			AddRailCharacterByGender(enabledCharacters, animalOrder[animalIndex], BattleCharacterDatabase.CharacterGender.Male, seen, result);
			AddRailCharacterByGender(enabledCharacters, animalOrder[animalIndex], BattleCharacterDatabase.CharacterGender.Female, seen, result);
		}
		return result;
	}

	private static void AddRailCharacterByGender(
		List<BattleCharacterDatabase.BattleCharacterData> source,
		BattleCharacterDatabase.CharacterAnimalType animalType,
		BattleCharacterDatabase.CharacterGender gender,
		HashSet<string> seen,
		List<BattleCharacterDatabase.BattleCharacterData> result)
	{
		if (source == null || seen == null || result == null)
		{
			return;
		}
		for (int i = 0; i < source.Count; i++)
		{
			BattleCharacterDatabase.BattleCharacterData data = source[i];
			if (data == null || !data.IsEnabled || data.AnimalType != animalType || data.Gender != gender)
			{
				continue;
			}
			if (string.IsNullOrWhiteSpace(data.Id))
			{
				continue;
			}
			if (!seen.Add(data.Id))
			{
				continue;
			}
			result.Add(data);
			return;
		}
	}

	private void EnsureRailCardCount(int count, RectTransform railViewport)
	{
		while (rightRailCards.Count < count)
		{
			rightRailCards.Add(CreateRailProfileCard(railViewport, rightRailCards.Count));
		}
	}

	private RailProfileCard CreateRailProfileCard(RectTransform parent, int index)
	{
		GameObject rootObject = new GameObject("CharacterRailCard_" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		rootObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform root = rootObject.GetComponent<RectTransform>();
		Image background = rootObject.GetComponent<Image>();
		BattlePopupStyle.ApplyWindow(background, raycastTarget: true);
		RailProfileCard railCard = new RailProfileCard
		{
			Root = root,
			Background = background,
			PhotoMat = CreateRailImage(root, "PhotoMat"),
			Portrait = CreateRailImage(root, "Portrait"),
			AvatarFrame = CreateRailImage(root, "AvatarFrame"),
			NameText = CreateRailText(root, "NameText", 30f, FontStyles.Bold, TextAlignmentOptions.Center),
			ClassText = CreateRailText(root, "ClassText", 18f, FontStyles.Bold, TextAlignmentOptions.Center),
			StateBlock = CreateRailImage(root, "StateBlock"),
			PurchaseText = CreateRailText(root, "PurchaseText", 17f, FontStyles.Bold, TextAlignmentOptions.Center)
		};
		RailProfileCardHolder holder = rootObject.AddComponent<RailProfileCardHolder>();
		holder.Card = railCard;
		holder.Clicked = PreviewRailCharacter;
		return railCard;
	}

	private Image CreateRailImage(RectTransform parent, string name)
	{
		GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		obj.transform.SetParent(parent, worldPositionStays: false);
		Image image = obj.GetComponent<Image>();
		image.raycastTarget = false;
		return image;
	}

	private TMP_Text CreateRailText(RectTransform parent, string name, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
	{
		GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		obj.transform.SetParent(parent, worldPositionStays: false);
		TMP_Text text = obj.GetComponent<TMP_Text>();
		text.fontSize = fontSize;
		text.fontSizeMin = Mathf.Max(10f, fontSize - 7f);
		text.fontSizeMax = fontSize + 4f;
		text.enableAutoSizing = true;
		text.fontStyle = fontStyle;
		text.alignment = alignment;
		text.color = new Color(1f, 0.82f, 0.28f, 1f);
		text.raycastTarget = false;
		text.overflowMode = TextOverflowModes.Truncate;
		return text;
	}

	private void ApplyRailCard(RailProfileCard card, BattleCharacterDatabase.BattleCharacterData data, Vector2 position)
	{
		if (card == null || card.Root == null || data == null)
		{
			return;
		}
		card.CharacterId = data.Id;
		bool unlocked = !BattleCharacterSelectionService.HasInstance || BattleCharacterSelectionService.Instance.IsUnlocked(data.Id);
		int price = BattleCharacterSelectionService.HasInstance ? BattleCharacterSelectionService.Instance.GetUnlockPrice(data.Id) : Mathf.Max(0, data.PriceAmount);
		ApplyRailRect(card.Root, position, rightRailCardSize);
		ApplyRailRect(card.PhotoMat.rectTransform, new Vector2((0f - rightRailCardSize.x) * 0.28f, 18f), new Vector2(rightRailCardSize.y * 0.72f, rightRailCardSize.y * 0.72f));
		ApplyRailRect(card.Portrait.rectTransform, new Vector2((0f - rightRailCardSize.x) * 0.28f, 18f), new Vector2(rightRailCardSize.y * 0.6f, rightRailCardSize.y * 0.6f));
		ApplyRailRect(card.AvatarFrame.rectTransform, new Vector2((0f - rightRailCardSize.x) * 0.28f, 18f), new Vector2(rightRailCardSize.y * 0.8f, rightRailCardSize.y * 0.8f));
		ApplyRailRect(card.NameText.rectTransform, new Vector2(rightRailCardSize.x * 0.2f, rightRailCardSize.y * 0.2f), new Vector2(rightRailCardSize.x * 0.44f, 56f));
		ApplyRailRect(card.ClassText.rectTransform, new Vector2(rightRailCardSize.x * 0.2f, -22f), new Vector2(rightRailCardSize.x * 0.44f, 78f));
		ApplyRailRect(card.StateBlock.rectTransform, new Vector2(rightRailCardSize.x * 0.2f, (0f - rightRailCardSize.y) * 0.31f), new Vector2(rightRailCardSize.x * 0.44f, 48f));
		ApplyRailRect(card.PurchaseText.rectTransform, new Vector2(rightRailCardSize.x * 0.2f, (0f - rightRailCardSize.y) * 0.31f), new Vector2(rightRailCardSize.x * 0.44f, 42f));
		ApplyRailSprite(card.PhotoMat, LoadRailSprite(ref cachedRailPhotoSprite, "Mahjong/Sprites/BattleLobbyUI/InfoPanel"), sliced: true);
		ApplyRailSprite(card.Portrait, ResolveRailPortraitSprite(data), sliced: false);
		ApplyRailSprite(card.AvatarFrame, LoadRailSprite(ref cachedRailAvatarFrameSprite, "Mahjong/Sprites/BattleLobbyUI/AvatarFrameGenerated"), sliced: false);
		ApplyRailSprite(card.StateBlock, LoadRailSprite(ref cachedRailStateSprite, "Mahjong/Sprites/BattleLobbyParts/PartWideAlt"), sliced: true);
		card.StateBlock.color = unlocked ? new Color(0.35f, 0.30f, 0.12f, 0.92f) : new Color(0.28f, 0.08f, 0.05f, 0.94f);
		card.Portrait.color = Color.white;
		card.AvatarFrame.color = Color.white;
		card.NameText.text = data.LocalizedDisplayName;
		card.ClassText.text = BuildRailClassText(data);
		card.PurchaseText.text = unlocked ? GameLocalization.Text("battle.character.rail.owned") : BuildRailPurchaseText(data, price);
		card.PurchaseText.color = unlocked ? new Color(1f, 0.86f, 0.42f, 1f) : new Color(1f, 0.58f, 0.28f, 1f);
		card.Root.SetAsLastSibling();
	}

	private void EnsureRailScrollHint()
	{
		RectTransform root = base.transform as RectTransform;
		if (root == null)
		{
			return;
		}
		if (railScrollHintText == null)
		{
			Transform existing = root.Find("CharacterRailScrollHint");
			railScrollHintText = existing != null ? existing.GetComponent<TMP_Text>() : null;
		}
		if (railScrollHintText != null)
		{
			railScrollHintText.gameObject.SetActive(value: false);
		}
		float railHeight = rightRailCardSize.y * 2f + rightRailCardGap;
		const float buttonGap = 36f;
		railScrollUpButton = EnsureRailScrollButton(root, "CharacterRailScrollUpButton", "▲", new Vector2(rightRailFramePosition.x, rightRailFramePosition.y + railHeight * 0.5f + buttonGap), -1);
		railScrollDownButton = EnsureRailScrollButton(root, "CharacterRailScrollDownButton", "▼", new Vector2(rightRailFramePosition.x, rightRailFramePosition.y - railHeight * 0.5f - buttonGap), 1);
		railScrollUpButton.transform.SetAsLastSibling();
		railScrollDownButton.transform.SetAsLastSibling();
	}

	private Button EnsureRailScrollButton(RectTransform root, string objectName, string label, Vector2 position, int direction)
	{
		Transform existing = root.Find(objectName);
		GameObject obj = existing != null ? existing.gameObject : new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		if (existing == null)
		{
			obj.transform.SetParent(root, worldPositionStays: false);
		}
		RectTransform rect = obj.GetComponent<RectTransform>();
		ApplyRailRect(rect, position, new Vector2(132f, 66f));
		Image image = obj.GetComponent<Image>();
		Button button = obj.GetComponent<Button>();
		button.targetGraphic = image;
		BattlePopupStyle.ApplyPremiumButton(button);
		button.onClick.RemoveAllListeners();
		button.onClick.AddListener(delegate
		{
			ScrollRailPage(direction);
		});
		TMP_Text text = obj.GetComponentInChildren<TMP_Text>();
		if (text == null)
		{
			GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
			textObject.transform.SetParent(rect, worldPositionStays: false);
			text = textObject.GetComponent<TMP_Text>();
		}
		RectTransform textRect = text.rectTransform;
		textRect.anchorMin = Vector2.zero;
		textRect.anchorMax = Vector2.one;
		textRect.offsetMin = new Vector2(22f, 8f);
		textRect.offsetMax = new Vector2(-22f, -8f);
		text.text = label;
		text.fontSize = 40f;
		text.fontSizeMin = 30f;
		text.fontSizeMax = 46f;
		text.enableAutoSizing = true;
		text.fontStyle = FontStyles.Bold;
		text.alignment = TextAlignmentOptions.Center;
		text.color = new Color(1f, 0.82f, 0.28f, 1f);
		text.raycastTarget = false;
		text.gameObject.SetActive(false);
		EnsureRailScrollChevron(rect, direction);
		obj.SetActive(value: true);
		return button;
	}

	private static void EnsureRailScrollChevron(RectTransform parent, int direction)
	{
		Transform existing = parent.Find("ChevronIcon");
		GameObject iconObject = existing != null
			? existing.gameObject
			: new GameObject("ChevronIcon", typeof(RectTransform));
		if (existing == null)
		{
			iconObject.transform.SetParent(parent, worldPositionStays: false);
		}
		RectTransform iconRoot = iconObject.GetComponent<RectTransform>();
		ApplyRailRect(iconRoot, Vector2.zero, new Vector2(58f, 40f));
		float leftRotation = direction < 0 ? 40f : -40f;
		float rightRotation = -leftRotation;
		EnsureRailScrollChevronWing(iconRoot, "LeftWing", new Vector2(-8f, 0f), leftRotation);
		EnsureRailScrollChevronWing(iconRoot, "RightWing", new Vector2(8f, 0f), rightRotation);
		iconObject.SetActive(true);
		iconRoot.SetAsLastSibling();
	}

	private static void EnsureRailScrollChevronWing(
		RectTransform parent,
		string objectName,
		Vector2 position,
		float rotation)
	{
		Image wing = EnsureFirstHeroDialogueImage(parent, objectName);
		ApplyRailRect(wing.rectTransform, position, new Vector2(30f, 8f));
		wing.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
		wing.enabled = true;
		wing.sprite = null;
		wing.type = Image.Type.Simple;
		wing.preserveAspect = false;
		wing.color = new Color(1f, 0.76f, 0.18f, 1f);
		wing.raycastTarget = false;
	}

	private void ScrollRailPage(int direction)
	{
		float railStep = GetRailPageStep();
		railScrollOffset = SnapRailScrollOffset(railScrollOffset + Mathf.Sign(direction) * railStep);
		UpdateVerticalRailPositions();
	}

	private static void ApplyRailRect(RectTransform rect, Vector2 position, Vector2 size)
	{
		if (rect == null)
		{
			return;
		}
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = position;
		rect.sizeDelta = size;
		rect.localScale = Vector3.one;
	}

	private static void ApplyRailSprite(Image image, Sprite sprite, bool sliced)
	{
		if (image == null)
		{
			return;
		}
		image.sprite = sprite;
		image.enabled = sprite != null;
		image.preserveAspect = !sliced;
		image.type = sliced && sprite != null && sprite.border.sqrMagnitude > 0.01f ? Image.Type.Sliced : Image.Type.Simple;
		image.color = Color.white;
		image.raycastTarget = false;
	}

	private static Sprite LoadRailSprite(ref Sprite cache, string path)
	{
		if (cache != null)
		{
			return cache;
		}
		cache = Resources.Load<Sprite>(path);
		if (cache == null)
		{
			Sprite[] sprites = Resources.LoadAll<Sprite>(path);
			cache = sprites != null && sprites.Length > 0 ? sprites[0] : null;
		}
		return cache;
	}

	private static Sprite ResolveRailPortraitSprite(BattleCharacterDatabase.BattleCharacterData data)
	{
		if (data == null)
		{
			return null;
		}
		return data.ProfileSprite != null ? data.ProfileSprite : (data.LobbySprite != null ? data.LobbySprite : data.BattleSprite);
	}

	private static string BuildRailClassText(BattleCharacterDatabase.BattleCharacterData data)
	{
		string classKey = data?.AnimalType switch
		{
			BattleCharacterDatabase.CharacterAnimalType.Tiger => "battle.character.rail.class.vanguard",
			BattleCharacterDatabase.CharacterAnimalType.Fox => "battle.character.rail.class.scout",
			BattleCharacterDatabase.CharacterAnimalType.Wolf => "battle.character.rail.class.duelist",
			BattleCharacterDatabase.CharacterAnimalType.Bear => "battle.character.rail.class.sentinel",
			BattleCharacterDatabase.CharacterAnimalType.Dragon => "battle.character.rail.class.arcanist",
			BattleCharacterDatabase.CharacterAnimalType.Dog => "battle.character.rail.class.tracker",
			_ => "battle.character.rail.class.fighter"
		};
		return GameLocalization.Format("battle.character.rail.class", GameLocalization.Text(classKey));
	}

	private static string BuildRailPurchaseText(BattleCharacterDatabase.BattleCharacterData data, int price)
	{
		if (data == null)
		{
			return string.Empty;
		}
		if (price <= 0)
		{
			return GameLocalization.Text("battle.character.rail.first_free");
		}
		return GameLocalization.Format(
			"battle.character.rail.buy",
			FormatRailPrice(price),
			GetRailCurrencyName(ResolveRailUnlockCurrency(data)));
	}

	private static string FormatRailPrice(int value)
	{
		if (value >= 1000000)
		{
			return Mathf.FloorToInt(value / 1000000f) + "M";
		}
		if (value >= 1000)
		{
			return Mathf.FloorToInt(value / 1000f) + "K";
		}
		return value.ToString();
	}

	private static string GetRailCurrencyName(BattleCharacterDatabase.CharacterPriceCurrencyType currency)
	{
		switch (currency)
		{
			case BattleCharacterDatabase.CharacterPriceCurrencyType.OzAmetist:
				return GameLocalization.Text("common.oz_ametist");
			case BattleCharacterDatabase.CharacterPriceCurrencyType.OzAltin:
				return GameLocalization.Text("common.oz_altin");
			case BattleCharacterDatabase.CharacterPriceCurrencyType.OzTile:
				return "OzTile";
			default:
				return "OzTile";
		}
	}

	private static BattleCharacterDatabase.CharacterPriceCurrencyType ResolveRailUnlockCurrency(BattleCharacterDatabase.BattleCharacterData data)
	{
		if (data != null &&
			(data.AnimalType == BattleCharacterDatabase.CharacterAnimalType.Dragon ||
			 data.UnlockType == BattleCharacterDatabase.CharacterUnlockType.PremiumCurrency ||
			 data.PriceCurrency == BattleCharacterDatabase.CharacterPriceCurrencyType.OzAmetist))
		{
			return BattleCharacterDatabase.CharacterPriceCurrencyType.OzAmetist;
		}
		return BattleCharacterDatabase.CharacterPriceCurrencyType.OzTile;
	}

	private float WrapRailScrollOffset(float value)
	{
		int count = rightRailCharacterCount;
		if (count <= 0)
		{
			return 0f;
		}
		float railStep = rightRailCardSize.y + rightRailCardGap;
		float contentHeight = Mathf.Max(railStep, count * railStep);
		return Mathf.Repeat(value, contentHeight);
	}

	private float SnapRailScrollOffset(float value)
	{
		float railStep = GetRailPageStep();
		if (railStep <= 1f)
		{
			return WrapRailScrollOffset(value);
		}
		return WrapRailScrollOffset(Mathf.Round(value / railStep) * railStep);
	}

	private float ResolveWrappedRailCardY(int index, float firstRailY, float railStep, int count)
	{
		if (count <= 0 || railStep <= 1f)
		{
			return firstRailY;
		}
		float contentHeight = count * railStep;
		float wrappedDistance = Mathf.Repeat(index * railStep - railScrollOffset, contentHeight);
		return firstRailY - wrappedDistance;
	}

	private float GetRailPageStep()
	{
		return (rightRailCardSize.y + rightRailCardGap) * 2f;
	}

	private void PreviewRailCharacter(string characterId)
	{
		if (string.IsNullOrWhiteSpace(characterId))
		{
			return;
		}
		hasPreviewedFirstHero = true;
		RefreshFirstHeroDialogue();
		previewCharacterId = characterId;
		centeredButton = FindButtonById(previewCharacterId) ?? centeredButton;
		UpdateButtonPositions();
		UpdateVisualStates();
	}

	private int CountAvailableRailButtons()
	{
		BattleCharacterButton mainButton = centeredButton ?? FindButtonForCurrentSelection();
		int count = 0;
		for (int i = 0; i < buttons.Count; i++)
		{
			BattleCharacterButton button = buttons[i];
			if (button != null && button != mainButton && IsCharacterAvailable(button))
			{
				count++;
			}
		}
		return count;
	}

	private RectTransform EnsureRightRailViewport()
	{
		RectTransform root = base.transform as RectTransform;
		if (root == null)
		{
			return null;
		}
		if (rightRailViewport == null)
		{
			Transform existing = root.Find("CharacterSelectRightRailViewport");
			rightRailViewport = existing as RectTransform;
		}
		if (rightRailViewport == null)
		{
			GameObject obj = new GameObject("CharacterSelectRightRailViewport", typeof(RectTransform), typeof(RectMask2D));
			obj.transform.SetParent(root, worldPositionStays: false);
			rightRailViewport = obj.GetComponent<RectTransform>();
		}
		rightRailViewport.anchorMin = new Vector2(0.5f, 0.5f);
		rightRailViewport.anchorMax = new Vector2(0.5f, 0.5f);
		rightRailViewport.pivot = new Vector2(0.5f, 0.5f);
		rightRailViewport.anchoredPosition = rightRailFramePosition;
		rightRailViewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rightRailCardSize.x + 16f);
		rightRailViewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rightRailCardSize.y * 2f + rightRailCardGap);
		rightRailViewport.localScale = Vector3.one;
		rightRailMask = rightRailViewport.GetComponent<RectMask2D>();
		if (rightRailMask == null)
		{
			rightRailMask = rightRailViewport.gameObject.AddComponent<RectMask2D>();
		}
		return rightRailViewport;
	}

	private static bool IsCharacterAvailable(BattleCharacterButton button)
	{
		if (button == null || string.IsNullOrWhiteSpace(button.CharacterId))
		{
			return false;
		}
		if (BattleCharacterSelectionService.HasInstance)
		{
			return BattleCharacterSelectionService.Instance.IsUnlocked(button.CharacterId);
		}
		return true;
	}

	private void DisableNavigationButton(string objectName, ref Button button)
	{
		RectTransform rectTransform = base.transform as RectTransform;
		Transform transform = ((rectTransform != null) ? rectTransform.Find(objectName) : null);
		button = ((transform != null) ? transform.GetComponent<Button>() : null);
		if (transform != null)
		{
			transform.gameObject.SetActive(false);
		}
	}

	private Button EnsureNavigationButton(string objectName, string labelText, Vector2 anchoredPosition, UnityAction action)
	{
		RectTransform rectTransform = base.transform as RectTransform;
		Transform transform = ((rectTransform != null) ? rectTransform.Find(objectName) : null);
		GameObject gameObject = ((transform != null) ? transform.gameObject : new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)));
		if (transform == null)
		{
			gameObject.transform.SetParent(base.transform, worldPositionStays: false);
		}
		RectTransform rectTransform2 = gameObject.transform as RectTransform;
		if (rectTransform2 != null)
		{
			rectTransform2.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform2.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform2.pivot = new Vector2(0.5f, 0.5f);
			rectTransform2.anchoredPosition = anchoredPosition;
			rectTransform2.sizeDelta = new Vector2(310f, 76f);
			rectTransform2.localScale = Vector3.one;
		}
		Button button = gameObject.GetComponent<Button>();
		if (button == null)
		{
			button = gameObject.AddComponent<Button>();
		}
		Image image = gameObject.GetComponent<Image>();
		if (image == null)
		{
			image = gameObject.AddComponent<Image>();
		}
		button.targetGraphic = image;
		button.onClick.RemoveListener(action);
		button.onClick.AddListener(action);
		button.interactable = true;
		BattlePopupStyle.ApplyButton(button);
		TMP_Text tMP_Text = gameObject.GetComponentInChildren<TMP_Text>(includeInactive: true);
		if (tMP_Text == null)
		{
			GameObject obj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
			obj.transform.SetParent(gameObject.transform, worldPositionStays: false);
			tMP_Text = obj.GetComponent<TMP_Text>();
		}
		RectTransform rectTransform3 = tMP_Text.rectTransform;
		if (rectTransform3 != null)
		{
			rectTransform3.anchorMin = Vector2.zero;
			rectTransform3.anchorMax = Vector2.one;
			rectTransform3.offsetMin = new Vector2(54f, 10f);
			rectTransform3.offsetMax = new Vector2(-54f, -10f);
			rectTransform3.localScale = Vector3.one;
		}
		tMP_Text.text = labelText;
		tMP_Text.raycastTarget = false;
		tMP_Text.alignment = TextAlignmentOptions.Center;
		tMP_Text.enableAutoSizing = true;
		tMP_Text.fontSize = 28f;
		tMP_Text.fontSizeMin = 16f;
		tMP_Text.fontSizeMax = 32f;
		tMP_Text.textWrappingMode = TextWrappingModes.NoWrap;
		tMP_Text.overflowMode = TextOverflowModes.Truncate;
		BattlePopupStyle.ApplyText(tMP_Text, silver: true);
		gameObject.transform.SetAsLastSibling();
		return button;
	}

	private void StepPreviousCharacter()
	{
		Step(-1);
	}

	private void StepNextCharacter()
	{
		Step(1);
	}

	private void BringNavigationButtonsToFront()
	{
		if (previousCharacterButton != null)
		{
			previousCharacterButton.transform.SetAsLastSibling();
		}
		if (nextCharacterButton != null)
		{
			nextCharacterButton.transform.SetAsLastSibling();
		}
	}

	private void UpdateCenteredButton()
	{
		centeredButton = null;
		int value = Mathf.RoundToInt(visualVirtualIndex);
		value = Mod(value, buttons.Count);
		for (int i = 0; i < buttons.Count; i++)
		{
			BattleCharacterButton battleCharacterButton = buttons[i];
			if (!(battleCharacterButton == null) && i == value)
			{
				centeredButton = battleCharacterButton;
				break;
			}
		}
		if (centeredButton != null && centeredButton.RectTransform != null)
		{
			centeredButton.RectTransform.SetAsLastSibling();
		}
		BringNavigationButtonsToFront();
	}

	private void UpdateVisualStates()
	{
		if (useRightVerticalCharacterRail)
		{
			UpdateVerticalRailVisualStates();
			return;
		}
		for (int i = 0; i < buttons.Count; i++)
		{
			BattleCharacterButton battleCharacterButton = buttons[i];
			if (battleCharacterButton == null || battleCharacterButton.RectTransform == null)
			{
				continue;
			}
			float num = Mathf.Abs(ResolveSymmetricSlot(i, buttons.Count));
			bool flag = ((visibleSideCount <= 0) ? (battleCharacterButton == centeredButton) : (num <= (float)visibleSideCount + 0.35f));
			float t = Mathf.Clamp01(num / Mathf.Max(1f, visibleSideCount));
			if (animateScale)
			{
				float num2 = (flag ? Mathf.Lerp(centerScale, sideScale, t) : sideScale);
				Vector3 b = new Vector3(num2, num2, 1f);
				battleCharacterButton.RectTransform.localScale = Vector3.Lerp(battleCharacterButton.RectTransform.localScale, b, Time.unscaledDeltaTime * scaleLerpSpeed);
			}
			if (canvasGroups.TryGetValue(battleCharacterButton, out var value))
			{
				float b2 = (flag ? Mathf.Lerp(centerAlpha, sideAlpha, t) : hiddenAlpha);
				if (!animateAlpha)
				{
					b2 = (flag ? 1f : 0f);
				}
				value.alpha = Mathf.Lerp(value.alpha, b2, Time.unscaledDeltaTime * alphaLerpSpeed);
				value.blocksRaycasts = flag && num <= (float)visibleSideCount + 0.05f;
				value.interactable = value.blocksRaycasts;
			}
			battleCharacterButton.SetHighlighted(battleCharacterButton == centeredButton);
		}
		for (int j = 0; j < buttons.Count; j++)
		{
			BattleCharacterButton battleCharacterButton2 = buttons[j];
			if (battleCharacterButton2 != null && battleCharacterButton2 != centeredButton && battleCharacterButton2.RectTransform != null)
			{
				battleCharacterButton2.RectTransform.SetAsFirstSibling();
			}
		}
		if (centeredButton != null && centeredButton.RectTransform != null)
		{
			centeredButton.RectTransform.SetAsLastSibling();
		}
		BringNavigationButtonsToFront();
	}

	private void UpdateVerticalRailVisualStates()
	{
		BattleCharacterButton mainButton = FindButtonById(previewCharacterId) ?? FindButtonForCurrentSelection();
		for (int i = 0; i < buttons.Count; i++)
		{
			BattleCharacterButton button = buttons[i];
			if (button == null || button.RectTransform == null)
			{
				continue;
			}
			bool isMain = button == mainButton;
			bool available = isMain || IsCharacterAvailable(button);
			button.RectTransform.localScale = Vector3.Lerp(button.RectTransform.localScale, Vector3.one, Time.unscaledDeltaTime * scaleLerpSpeed);
			if (canvasGroups.TryGetValue(button, out CanvasGroup group))
			{
				float targetAlpha = available ? 1f : 0f;
				group.alpha = Mathf.Lerp(group.alpha, targetAlpha, Time.unscaledDeltaTime * alphaLerpSpeed);
				group.blocksRaycasts = available;
				group.interactable = available;
			}
			button.SetHighlighted(isMain);
		}
		if (fullscreenBackground != null)
		{
			fullscreenBackground.transform.SetAsFirstSibling();
		}
		if (mainButton != null && mainButton.RectTransform != null)
		{
			mainButton.RectTransform.SetAsLastSibling();
		}
		for (int i = 0; i < buttons.Count; i++)
		{
			BattleCharacterButton button = buttons[i];
			if (button != null && button != mainButton && IsCharacterAvailable(button) && button.RectTransform != null)
			{
				button.RectTransform.SetAsLastSibling();
			}
		}
		BringNavigationButtonsToFront();
	}

	private void TryAutoSelectCentered()
	{
		if (useRightVerticalCharacterRail)
			return;

		if (autoSelectCenteredCharacter && !(centeredButton == null) && !isDragging && !isSnapping && !(Time.unscaledTime - snappedTime < selectDelayAfterSnap))
		{
			string characterId = centeredButton.CharacterId;
			if (!string.IsNullOrWhiteSpace(characterId) && !string.Equals(lastSelectedCenteredId, characterId, StringComparison.Ordinal) && BattleCharacterSelectionService.HasInstance && BattleCharacterSelectionService.Instance.IsUnlocked(characterId))
			{
				BattleCharacterSelectionService.Instance.SelectCharacter(characterId);
				lastSelectedCenteredId = characterId;
			}
		}
	}

	private BattleCharacterButton FindButtonForCurrentSelection()
	{
		if (!BattleCharacterSelectionService.HasInstance)
		{
			return null;
		}
		string selectedCharacterId = BattleCharacterSelectionService.Instance.SelectedCharacterId;
		if (string.IsNullOrWhiteSpace(selectedCharacterId))
		{
			return null;
		}
		for (int i = 0; i < buttons.Count; i++)
		{
			if (!(buttons[i] == null) && string.Equals(buttons[i].CharacterId, selectedCharacterId, StringComparison.Ordinal))
			{
				return buttons[i];
			}
		}
		return null;
	}

	private static float WrapDelta(float value, int count)
	{
		if (count <= 0)
		{
			return 0f;
		}
		float num = (float)count * 0.5f;
		return Mathf.Repeat(value + num, count) - num;
	}

	private float ResolveSymmetricSlot(int index, int count)
	{
		if (count <= 0)
		{
			return 0f;
		}
		float value = WrapDelta((float)index - visualVirtualIndex, count);
		float num = (float)visibleSideCount + 1f;
		return Mathf.Clamp(value, 0f - num, num);
	}

	private static int Mod(int value, int count)
	{
		if (count <= 0)
		{
			return 0;
		}
		int num = value % count;
		if (num >= 0)
		{
			return num;
		}
		return num + count;
	}

	private void OnSelectedCharacterChanged(string _)
	{
		RefreshFirstHeroDialogue();
		if (!isDragging)
		{
			SnapToSelectedOrFirst(instant: false);
		}
	}

	private IEnumerator RefreshNextFrame()
	{
		for (int i = 0; i < 3; i++)
		{
			yield return null;
			RefreshButtons();
			SnapToSelectedOrFirst(i == 0);
		}
	}

	private void OnCharacterCatalogChanged()
	{
		RefreshButtons();
		SnapToSelectedOrFirst(instant: false);
	}
}

internal sealed class RailProfileCard
{
	public string CharacterId;

	public RectTransform Root;

	public Image Background;

	public Image PhotoMat;

	public Image Portrait;

	public Image AvatarFrame;

	public TMP_Text NameText;

	public TMP_Text ClassText;

	public Image StateBlock;

	public TMP_Text PurchaseText;
}

internal sealed class RailProfileCardHolder : MonoBehaviour, IPointerClickHandler
{
	[NonSerialized]
	public RailProfileCard Card;

	[NonSerialized]
	public Action<string> Clicked;

	public void OnPointerClick(PointerEventData eventData)
	{
		if (Card == null || string.IsNullOrWhiteSpace(Card.CharacterId))
		{
			return;
		}
		Clicked?.Invoke(Card.CharacterId);
	}
}
}
