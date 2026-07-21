using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{

[DisallowMultipleComponent]
public sealed class BattleStoneForgeUI : MonoBehaviour
{
	private enum ForgeTab
	{
		Combine,
		Furnace
	}

	private const string LobbySceneName = "LobbyMahjongBattle";

	private const string HostObjectName = "BattleStoneForgeUI";

	private const string RuntimeCanvasName = "BattleStoneForgeCanvas";

	private const string OpenButtonName = "ButtonBattleStoneForge";

	private const string OverlayName = "BattleStoneForgeOverlay";

	private const string OzTileIconResourcePath = "Mahjong/Sprites/BattleTiles/OzTile";

	private const string ForgeHammerClipResourcePath = "Mahjong/Sounds/ForgeHammerHit";

	private const string VictoryClipResourcePath = "Mahjong/Sounds/game-won";

	private const int RuntimeCanvasSortingOrder = 30042;

	private const float ForgeHammerVolume = 0.82f;

	private const float ForgeVictoryVolume = 0.88f;

	private static readonly Vector2 OpenButtonSize = new Vector2(340f, 92f);

	private static Sprite cachedOzTileIcon;

	private static Sprite cachedForgeGlowSprite;

	private static AudioClip cachedForgeHammerClip;

	private static AudioClip cachedVictoryClip;

	private static bool tutorialForgeActive;

	private static string tutorialForgeTileId = string.Empty;

	private Canvas rootCanvas;

	private Button openButton;

	private GameObject overlayRoot;

	private Transform listRoot;

	private Transform stoneListContent;

	private Transform forgeSlotRoot;

	private Transform furnaceRoot;

	private TMP_Text titleText;

	private TMP_Text statusText;

	private TMP_Text resultText;

	private TMP_Text costText;

	private Image costIcon;

	private Button combineTabButton;

	private Button furnaceTabButton;

	private Button forgeButton;

	private Button ascendLegendaryButton;

	private Button ascendMythicButton;

	private TMP_Text furnaceInfoText;

	private GameObject tutorialResultRoot;

	private GameObject ascendSelectionRoot;

	private GameObject ascendResultRoot;

	private Transform ascendSelectionContent;

	private Transform ascendSelectedContent;

	private TMP_Text ascendSelectionCounterText;

	private TMP_Text ascendSelectionCostText;

	private TMP_Text ascendSelectionChanceText;

	private TMP_Text ascendSelectionFeedbackText;

	private Button ascendSelectionConfirmButton;

	private readonly List<BattleTileInventoryService.ForgeAscendSacrifice> ascendSelectedSacrifices = new List<BattleTileInventoryService.ForgeAscendSacrifice>();

	private BattleTileRarity ascendSelectionTargetRarity;

	private bool ascendProcessPlaying;

	private Coroutine ascendProcessCoroutine;

	private Coroutine ascendFeedbackHideCoroutine;

	private AudioSource forgeAudioSource;

	private string selectedTileId = string.Empty;

	private int selectedTileLevel;

	private ForgeTab currentTab;

	private bool forgeAnimationPlaying;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Initialize()
	{
		SceneManager.sceneLoaded -= HandleSceneLoaded;
		SceneManager.sceneLoaded += HandleSceneLoaded;
		EnsureForScene(SceneManager.GetActiveScene());
	}

	private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		EnsureForScene(scene);
	}

	private static void EnsureForScene(Scene scene)
	{
		if (!scene.IsValid() || !string.Equals(scene.name, "LobbyMahjongBattle", StringComparison.Ordinal))
		{
			return;
		}
		BattleStoneForgeUI[] array = UnityEngine.Object.FindObjectsByType<BattleStoneForgeUI>(FindObjectsInactive.Include);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] != null && array[i].gameObject.scene == scene)
			{
				return;
			}
		}
		Canvas orCreateRuntimeCanvas = GetOrCreateRuntimeCanvas(scene);
		if (!(orCreateRuntimeCanvas == null))
		{
			GameObject obj = new GameObject("BattleStoneForgeUI", typeof(RectTransform), typeof(BattleStoneForgeUI));
			SceneManager.MoveGameObjectToScene(obj, scene);
			obj.transform.SetParent(orCreateRuntimeCanvas.transform, worldPositionStays: false);
		}
	}

	public static bool TryOpenTutorialForge(string tileId)
	{
		Scene activeScene = SceneManager.GetActiveScene();
		if (!activeScene.IsValid() || !string.Equals(activeScene.name, "LobbyMahjongBattle", StringComparison.Ordinal))
		{
			return false;
		}
		EnsureForScene(activeScene);
		BattleStoneForgeUI[] array = UnityEngine.Object.FindObjectsByType<BattleStoneForgeUI>(FindObjectsInactive.Include);
		foreach (BattleStoneForgeUI battleStoneForgeUI in array)
		{
			if (!(battleStoneForgeUI == null) && !(battleStoneForgeUI.gameObject.scene != activeScene))
			{
				battleStoneForgeUI.OpenTutorialWindow(tileId);
				return true;
			}
		}
		return false;
	}

	private void Awake()
	{
		EnsureProfileServices();
		rootCanvas = GetComponentInParent<Canvas>();
		EnsureOpenButton();
	}

	private void OnEnable()
	{
		ProfileService.ProfileChanged += RefreshIfVisible;
		CurrencyService.CurrencyChanged += RefreshIfVisible;
		EnsureOpenButton();
	}

	private void OnDisable()
	{
		ProfileService.ProfileChanged -= RefreshIfVisible;
		CurrencyService.CurrencyChanged -= RefreshIfVisible;
		CloseAscendSelection();
		CloseAscendResult();
	}

	private void OnDestroy()
	{
		if (forgeAudioSource != null)
		{
			forgeAudioSource.Stop();
		}
		DestroyRuntimeObject((openButton != null) ? openButton.gameObject : null);
		DestroyRuntimeObject(overlayRoot);
		openButton = null;
		overlayRoot = null;
	}

	private void EnsureOpenButton()
	{
		if (!(openButton != null))
		{
			if (rootCanvas == null)
			{
				rootCanvas = GetOrCreateRuntimeCanvas(base.gameObject.scene);
			}
			if (!(rootCanvas == null))
			{
				Canvas buttonCanvas = FindBattleLobbyOpenButtonCanvas(base.gameObject.scene) ?? rootCanvas;
				GameObject gameObject = new GameObject("ButtonBattleStoneForge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
				gameObject.transform.SetParent(buttonCanvas.transform, worldPositionStays: false);
				MainLobbyUiCoordinator.LayoutBattleLobbyTopTabButton(gameObject.GetComponent<Button>(), 2, 4, GetCanvasSize(buttonCanvas));
				Image component = gameObject.GetComponent<Image>();
				component.color = Color.white;
				openButton = gameObject.GetComponent<Button>();
				openButton.targetGraphic = component;
				openButton.onClick.AddListener(OpenWindow);
				BattlePopupStyle.ApplyButton(openButton);
				CreateText(gameObject.transform, "Label", GameLocalization.Text("battle.lobby.forge"), Vector2.zero, OpenButtonSize, 34f, TextAlignmentOptions.Center).raycastTarget = false;
				BattlePopupStyle.ApplyBattleLobbyUtilityButton(openButton);
			}
		}
		RefreshOpenButtonLabel();
	}

	private void RefreshOpenButtonLabel()
	{
		TMP_Text label = (openButton != null) ? openButton.GetComponentInChildren<TMP_Text>(includeInactive: true) : null;
		if (label != null)
		{
			label.text = GameLocalization.Text("battle.lobby.forge");
		}
	}

	private static Vector2 GetCanvasSize(Canvas canvas)
	{
		RectTransform rectTransform = ((canvas != null) ? (canvas.transform as RectTransform) : null);
		if (rectTransform != null && rectTransform.rect.width > 1f && rectTransform.rect.height > 1f)
		{
			return rectTransform.rect.size;
		}
		return MainLobbyUiCoordinator.OverlayReferenceResolution;
	}

	private static Canvas FindBattleLobbyOpenButtonCanvas(Scene scene)
	{
		if (!scene.IsValid() || !string.Equals(scene.name, "LobbyMahjongBattle", StringComparison.Ordinal))
		{
			return null;
		}
		Canvas best = null;
		float bestArea = 0f;
		Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
		foreach (Canvas candidate in canvases)
		{
			if (candidate == null || candidate.gameObject.scene != scene || string.Equals(candidate.gameObject.name, "BattleStoneAuctionCanvas", StringComparison.Ordinal) || string.Equals(candidate.gameObject.name, "BattleStoneForgeCanvas", StringComparison.Ordinal))
			{
				continue;
			}
			RectTransform rect = candidate.transform as RectTransform;
			if (rect != null && rect.rect.width > 1f && rect.rect.height > 1f)
			{
				float area = rect.rect.width * rect.rect.height;
				if (area > bestArea)
				{
					best = candidate;
					bestArea = area;
				}
			}
		}
		return best;
	}

	private void OpenWindow()
	{
		CloseAscendSelection();
		CloseAscendResult();
		tutorialForgeActive = false;
		tutorialForgeTileId = string.Empty;
		BattleLobbyUiCoordinator.OpenModal(BattleLobbyModalKind.Forge);
		EnsureProfileServices();
		EnsureOverlay();
		overlayRoot.SetActive(value: true);
		overlayRoot.transform.SetAsLastSibling();
		selectedTileId = string.Empty;
		selectedTileLevel = 0;
		currentTab = ForgeTab.Combine;
		RefreshWindow();
	}

	private void OpenTutorialWindow(string tileId)
	{
		CloseAscendSelection();
		CloseAscendResult();
		tutorialForgeActive = true;
		tutorialForgeTileId = tileId ?? string.Empty;
		BattleLobbyUiCoordinator.OpenModal(BattleLobbyModalKind.Forge);
		EnsureProfileServices();
		EnsureOverlay();
		overlayRoot.SetActive(value: true);
		overlayRoot.transform.SetAsLastSibling();
		currentTab = ForgeTab.Combine;
		selectedTileId = string.Empty;
		selectedTileLevel = 0;
		RefreshWindow();
		SetTutorialStatus();
	}

	private void CloseWindow()
	{
		if (tutorialForgeActive && BattleLoreTutorialSession.IsActive && BattleLoreTutorialSession.ActiveStage == 4)
		{
			SetTutorialStatus();
			return;
		}
		if (overlayRoot != null)
		{
			overlayRoot.SetActive(value: false);
		}
		CloseAscendSelection();
		CloseAscendResult();
		tutorialForgeActive = false;
		tutorialForgeTileId = string.Empty;
		BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.Forge);
	}

	private void EnsureOverlay()
	{
		if (!(overlayRoot != null))
		{
			Canvas orCreateRuntimeCanvas = GetOrCreateRuntimeCanvas(base.gameObject.scene);
			overlayRoot = new GameObject("BattleStoneForgeOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			overlayRoot.transform.SetParent(orCreateRuntimeCanvas.transform, worldPositionStays: false);
			RectTransform component = overlayRoot.GetComponent<RectTransform>();
			component.anchorMin = Vector2.zero;
			component.anchorMax = Vector2.one;
			component.offsetMin = Vector2.zero;
			component.offsetMax = Vector2.zero;
			Image component2 = overlayRoot.GetComponent<Image>();
			component2.color = new Color(0f, 0f, 0f, 0.68f);
			component2.raycastTarget = true;
			GameObject gameObject = CreatePanel(overlayRoot.transform, "ForgePanel", new Vector2(2300f, 1030f), Vector2.zero);
			titleText = CreateText(gameObject.transform, "Title", string.Empty, new Vector2(0f, 446f), new Vector2(1500f, 58f), 52f, TextAlignmentOptions.Center);
			statusText = CreateText(gameObject.transform, "Status", string.Empty, new Vector2(0f, -438f), new Vector2(1800f, 76f), 28f, TextAlignmentOptions.Center);
			combineTabButton = CreateButton(gameObject.transform, "TabCombineForge", T("Закалка", "Tempering", "Güçlendirme", "Verstärken"), new Vector2(-230f, 350f), new Vector2(330f, 68f), delegate
			{
				SelectTab(ForgeTab.Combine);
			});
			furnaceTabButton = CreateButton(gameObject.transform, "TabFurnace", T("Возвышение", "Ascension", "Yükseltme", "Aufstieg"), new Vector2(130f, 350f), new Vector2(300f, 68f), delegate
			{
				SelectTab(ForgeTab.Furnace);
			});
			listRoot = CreatePanel(gameObject.transform, "StoneList", new Vector2(980f, 700f), new Vector2(-560f, -24f)).transform;
			CreateText(listRoot, "ListTitle", T("Камни", "Stones", "Taşlar", "Steine"), new Vector2(0f, 300f), new Vector2(760f, 42f), 36f, TextAlignmentOptions.Center);
			stoneListContent = CreateScrollContent(listRoot, "StoneScroll", new Vector2(0f, -30f), new Vector2(840f, 530f));
			GameObject gameObject2 = CreatePanel(gameObject.transform, "ForgeSlots", new Vector2(900f, 700f), new Vector2(575f, -24f));
			forgeSlotRoot = gameObject2.transform;
			TMP_Text forgeTitle = CreateText(forgeSlotRoot, "ForgeTitle", T("Три камня — одно усиление", "Three stones — one upgrade", "Üç taş — bir güçlendirme", "Drei Steine — eine Verbesserung"), new Vector2(0f, 300f), new Vector2(760f, 42f), 34f, TextAlignmentOptions.Center);
			forgeTitle.color = new Color(1f, 0.82f, 0.46f, 1f);
			TMP_Text forgeGuide = CreateText(forgeSlotRoot, "ForgeGuide", T("Выбери камень слева: три одинаковые копии заполнят слоты.", "Choose a stone on the left: three identical copies will fill the slots.", "Soldan bir taş seç: üç aynı kopya yuvaları doldurur.", "Waehle links einen Stein: drei gleiche Kopien fuellen die Plaetze."), new Vector2(0f, 256f), new Vector2(760f, 40f), 23f, TextAlignmentOptions.Center);
			forgeGuide.color = new Color(0.9f, 0.78f, 0.58f, 0.96f);
			resultText = CreateText(forgeSlotRoot, "Result", string.Empty, new Vector2(0f, -92f), new Vector2(780f, 128f), 27f, TextAlignmentOptions.Center);
			TMP_Text costCaption = CreateText(forgeSlotRoot, "CostCaption", T("Стоимость закалки", "Forging cost", "Dövme bedeli", "Schmiedekosten"), new Vector2(0f, -164f), new Vector2(300f, 30f), 21f, TextAlignmentOptions.Center);
			costCaption.color = new Color(0.82f, 0.72f, 0.56f, 0.94f);
			costIcon = CreateImage(forgeSlotRoot, "CostIcon", new Vector2(-70f, -202f), new Vector2(46f, 46f));
			costIcon.sprite = LoadOzTileIcon();
			costIcon.enabled = costIcon.sprite != null;
			costText = CreateText(forgeSlotRoot, "CostText", "0", new Vector2(38f, -202f), new Vector2(160f, 44f), 32f, TextAlignmentOptions.MidlineLeft);
			forgeButton = CreateButton(forgeSlotRoot, "ButtonForgeCombine", T("Соединить", "Combine", "Birleştir", "Verbinden"), new Vector2(0f, -270f), new Vector2(420f, 72f), ForgeSelectedTile);
			GameObject gameObject3 = CreatePanel(gameObject.transform, "FurnaceTabPanel", new Vector2(1900f, 700f), new Vector2(0f, -24f));
			furnaceRoot = gameObject3.transform;
			CreateText(furnaceRoot, "FurnaceTitle", T("Возвышение камней", "Stone Ascension", "Taş Yükseltme", "Steinaufstieg"), new Vector2(0f, 266f), new Vector2(1100f, 58f), 44f, TextAlignmentOptions.Center);
			furnaceInfoText = CreateText(furnaceRoot, "FurnaceInfo", string.Empty, new Vector2(0f, 126f), new Vector2(1340f, 130f), 30f, TextAlignmentOptions.Center);
			ascendLegendaryButton = CreateButton(furnaceRoot, "ButtonAscendLegendary", string.Empty, new Vector2(-360f, -154f), new Vector2(560f, 124f), delegate
			{
				AscendForge(BattleTileRarity.Legendary);
			});
			ascendMythicButton = CreateButton(furnaceRoot, "ButtonAscendMythic", string.Empty, new Vector2(360f, -154f), new Vector2(560f, 124f), delegate
			{
				AscendForge(BattleTileRarity.Mythic);
			});
			CreateButton(gameObject.transform, "ButtonCloseForge", T("Закрыть", "Close", "Kapat", "Schliessen"), new Vector2(1000f, 438f), new Vector2(190f, 70f), CloseWindow);
			overlayRoot.SetActive(value: false);
		}
	}

	private void RefreshIfVisible()
	{
		if (overlayRoot != null && overlayRoot.activeSelf)
		{
			RefreshWindow();
			if (ascendSelectionRoot != null && !ascendProcessPlaying)
			{
				RebuildAscendSelection();
			}
		}
	}

	private void RefreshWindow()
	{
		PlayerProfile profile = GetProfile();
		BattleTileStore battleTileStore = ((BattleTileStore.I != null) ? BattleTileStore.I : UnityEngine.Object.FindAnyObjectByType<BattleTileStore>());
		if (profile == null || battleTileStore == null)
		{
			statusText.text = T("Профиль или камни еще загружаются.", "Profile or stones are still loading.", "Profil veya taşlar yükleniyor.", "Profil oder Steine laden noch.");
			return;
		}
		BattleTileInventoryService.EnsureInventoryForStore(profile, battleTileStore);
		MahjongBattleTileInventoryData orCreateInventory = BattleTileInventoryService.GetOrCreateInventory(profile);
		titleText.text = ((currentTab == ForgeTab.Combine) ? T("Кузница камней", "Stone Forge", "Taş Ocağı", "Steinschmiede") : T("Возвышение камней", "Stone Ascension", "Taş Yükseltme", "Steinaufstieg"));
		RefreshTabs();
		if (currentTab == ForgeTab.Combine)
		{
			RebuildStoneList(orCreateInventory, battleTileStore);
			RebuildForgeSlots(profile, battleTileStore);
		}
		else
		{
			RefreshFurnaceTab(profile, battleTileStore);
		}
	}

	private void SelectTab(ForgeTab tab)
	{
		if (tutorialForgeActive && tab != ForgeTab.Combine)
		{
			SetTutorialStatus();
			return;
		}
		CloseAscendSelection();
		currentTab = tab;
		RefreshWindow();
	}

	private void RefreshTabs()
	{
		SetTabActive(combineTabButton, currentTab == ForgeTab.Combine);
		SetTabActive(furnaceTabButton, currentTab == ForgeTab.Furnace);
		if (tutorialForgeActive && furnaceTabButton != null)
		{
			furnaceTabButton.interactable = false;
		}
		if (listRoot != null)
		{
			listRoot.gameObject.SetActive(currentTab == ForgeTab.Combine);
		}
		if (forgeSlotRoot != null)
		{
			forgeSlotRoot.gameObject.SetActive(currentTab == ForgeTab.Combine);
		}
		if (furnaceRoot != null)
		{
			furnaceRoot.gameObject.SetActive(currentTab == ForgeTab.Furnace);
		}
	}

	private static void SetTabActive(Button button, bool active)
	{
		if (!(button == null) && !(button.image == null))
		{
			button.interactable = true;
			button.transform.localScale = active ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;
			button.image.color = (active ? new Color(1f, 0.78f, 0.32f, 1f) : new Color(0.82f, 0.8f, 0.74f, 1f));
			TMP_Text label = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
			if (label != null)
			{
				label.color = active ? new Color(1f, 0.9f, 0.54f, 1f) : new Color(0.92f, 0.9f, 0.84f, 1f);
				label.fontStyle = active ? label.fontStyle | FontStyles.Bold : label.fontStyle & ~FontStyles.Bold;
				label.outlineColor = active ? new Color(0.95f, 0.42f, 0.04f, 0.95f) : new Color(0.1f, 0.04f, 0.01f, 0.72f);
				label.outlineWidth = active ? 0.2f : 0.06f;
				Shadow glow = label.GetComponent<Shadow>();
				if (glow == null)
				{
					glow = label.gameObject.AddComponent<Shadow>();
				}
				glow.enabled = active;
				glow.effectColor = new Color(1f, 0.48f, 0.06f, 0.78f);
				glow.effectDistance = new Vector2(2f, -2f);
			}
		}
	}

	private void RebuildStoneList(MahjongBattleTileInventoryData inventory, BattleTileStore store)
	{
		if (stoneListContent == null)
		{
			return;
		}
		ClearDynamicChildren(stoneListContent);
		if (inventory?.TileStacks == null || inventory.TileStacks.Count == 0)
		{
			CreateText(stoneListContent, "Empty", T("Нет камней для кузницы.", "No stones for Forge.", "Forge için taş yok.", "Keine Steine fuer Forge."), new Vector2(0f, -40f), new Vector2(560f, 90f), 34f, TextAlignmentOptions.Center);
			return;
		}
		int num = 0;
		for (int i = 0; i < inventory.TileStacks.Count; i++)
		{
			MahjongBattleTileStackData mahjongBattleTileStackData = inventory.TileStacks[i];
			if (mahjongBattleTileStackData != null && mahjongBattleTileStackData.Count > 0 && store.TryGetTileDataById(mahjongBattleTileStackData.TileId, out var data) && data != null && data.Rarity >= BattleTileRarity.Rare && (!tutorialForgeActive || string.Equals(mahjongBattleTileStackData.TileId, tutorialForgeTileId, StringComparison.Ordinal)))
			{
				string id = mahjongBattleTileStackData.TileId;
				int level = Mathf.Max(0, mahjongBattleTileStackData.UpgradeLevel);
				bool flag = string.Equals(selectedTileId, id, StringComparison.Ordinal) && selectedTileLevel == level;
				int num2 = num % 5;
				int num3 = num / 5;
				CreateStoneRow(position: new Vector2(-320f + (float)num2 * 160f, -14f - (float)num3 * 190f), parent: stoneListContent, objectName: "Stone_" + num + "_L" + level, data: data, tileId: id, count: mahjongBattleTileStackData.Count, level: level, selected: flag, action: delegate
				{
					SelectTile(id, level);
				}).interactable = !flag;
				num++;
			}
		}
		if (num == 0)
		{
			CreateText(stoneListContent, "Empty", T("Кузница открыта для редких камней и выше.", "Forge opens for Rare and higher.", "Forge Rare ve üstü için.", "Forge ist fuer Rare und hoeher."), new Vector2(0f, -40f), new Vector2(560f, 100f), 32f, TextAlignmentOptions.Center);
		}
		RectTransform rectTransform = stoneListContent as RectTransform;
		if (rectTransform != null)
		{
			int num4 = Mathf.CeilToInt((float)num / 5f);
			rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, Mathf.Max(640f, (float)num4 * 190f + 16f));
		}
	}

	private void RebuildForgeSlots(PlayerProfile profile, BattleTileStore store)
	{
		ClearDynamicChildren(forgeSlotRoot, "ForgeTitle", "ForgeGuide", "Result", "CostCaption", "CostIcon", "CostText", "ButtonForgeCombine", "ButtonAscendLegendary", "ButtonAscendMythic");
		bool num = !string.IsNullOrWhiteSpace(selectedTileId);
		int num2 = (num ? BattleTileInventoryService.GetOwnedCount(profile, selectedTileId, selectedTileLevel) : 0);
		int num3 = (num ? selectedTileLevel : 0);
		BattleTileData data = null;
		if (num)
		{
			store.TryGetTileDataById(selectedTileId, out data);
		}
		for (int i = 0; i < 3; i++)
		{
			bool flag = i < num2;
			GameObject gameObject = CreatePanel(forgeSlotRoot, "ForgeSlot" + i, new Vector2(170f, 210f), new Vector2(-230f + (float)i * 230f, 116f));
			gameObject.GetComponent<Image>().raycastTarget = false;
			Image image2 = CreateImage(gameObject.transform, "StoneWell", Vector2.zero, new Vector2(136f, 172f));
			if (!BattlePopupStyle.ApplyFront(image2, raycastTarget: false))
			{
				image2.preserveAspect = false;
			}
			image2.color = flag && data != null ? ResolveForgeSlotColor(data.Rarity) : new Color(0.055f, 0.04f, 0.025f, 0.94f);
			Outline outline = image2.gameObject.AddComponent<Outline>();
			outline.effectColor = flag && data != null ? ResolveForgeSlotOutlineColor(data.Rarity) : new Color(0.5f, 0.34f, 0.13f, 0.72f);
			outline.effectDistance = new Vector2(2f, -2f);
			if (flag && data != null)
			{
				Image image3 = CreateImage(gameObject.transform, "StoneGlow", Vector2.zero, new Vector2(146f, 184f));
				image3.sprite = ResolveForgeGlowSprite();
				Color color = ResolveForgeSlotOutlineColor(data.Rarity);
				color.a = 0.32f;
				image3.color = color;
			}
			Image image = CreateImage(gameObject.transform, "Face", Vector2.zero, new Vector2(132f, 166f));
			image.sprite = ((flag && data?.Prefab != null) ? data.Prefab.FaceSprite : null);
			image.enabled = image.sprite != null;
			if (!image.enabled)
			{
				TMP_Text slotText = CreateText(gameObject.transform, "SlotText", flag ? ResolveTileName(data, selectedTileId) : "+", Vector2.zero, new Vector2(120f, 90f), flag ? 26f : 38f, TextAlignmentOptions.Center);
				slotText.color = flag ? Color.white : new Color(0.74f, 0.58f, 0.3f, 0.72f);
			}
			else
			{
				CreateForgeUpgradeStars(gameObject.transform, num3, Vector2.zero, new Vector2(132f, 166f));
			}
		}
		bool flag2 = IsActiveTutorialForgeTarget();
		int num4 = ((!flag2) ? BattleTileInventoryService.GetForgeOzTileCost(data, num3) : 0);
		int num5 = ((CurrencyService.I != null) ? CurrencyService.I.GetOzTile() : ((profile?.Currencies != null) ? profile.Currencies.OzTile : 0));
		bool flag3 = num4 <= 0 || num5 >= num4;
		bool flag4 = num2 >= 3;
		bool flag5 = data != null && data.Rarity >= BattleTileRarity.Rare && flag4 && flag3;
		resultText.text = ((data == null) ? T("Выбери камень в коллекции слева.\nТри одинаковые копии превратятся в одно усиление.", "Choose a stone from the collection on the left.\nThree identical copies become one upgrade.", "Soldaki koleksiyondan bir taş seç.\nÜç aynı kopya tek güçlendirmeye dönüşür.", "Waehle links einen Stein aus der Sammlung.\nDrei gleiche Kopien werden zu einer Verbesserung.") : FormatForgeResult(data, selectedTileId, num3, num2));
		Transform costCaptionTransform = forgeSlotRoot.Find("CostCaption");
		if (costCaptionTransform != null)
		{
			costCaptionTransform.gameObject.SetActive(flag4);
		}
		if (costText != null)
		{
			costText.text = ((flag4 && num4 > 0) ? num4.ToString() : string.Empty);
			costText.color = (flag3 ? Color.white : new Color(1f, 0.48f, 0.42f, 1f));
		}
		if (costIcon != null)
		{
			costIcon.sprite = LoadOzTileIcon();
			costIcon.enabled = flag4 && costIcon.sprite != null;
		}
		forgeButton.interactable = flag5 && !forgeAnimationPlaying;
		if (flag5)
		{
			statusText.text = (flag2 ? T("Урок кузницы: нажми «Соединить» и преврати 3 одинаковых камня в усиление +1.", "Forge lesson: press Combine and turn 3 identical stones into a +1 upgrade.", "Forge dersi: Birleştir'e bas ve 3 aynı taşı +1 güçlendirmeye dönüştür.", "Forge-Lektion: Druecke Verbinden und mache aus 3 gleichen Steinen ein +1 Upgrade.") : T("Огонь готов: две лишние копии исчезнут, а сила выбранного камня возрастёт на 10%.", "The fire is ready: two extra copies vanish and the chosen stone gains 10% power.", "Ateş hazır: iki fazla kopya kaybolur ve seçilen taş %10 güç kazanır.", "Das Feuer ist bereit: zwei Zusatzkopien verschwinden, der Stein erhaelt 10% Kraft."));
		}
		else if (data != null && !flag4)
		{
			statusText.text = T("Для закалки не хватает копий: собери три одинаковых камня.", "Not enough copies for forging: collect three identical stones.", "Dövme için kopya eksik: üç aynı taş topla.", "Zum Schmieden fehlen Kopien: sammle drei gleiche Steine.");
		}
		else if (data != null && !flag3)
		{
			statusText.text = string.Format("{0}: {1}/{2}", T("Не хватает OzTile", "Not enough OzTile", "OzTile yetersiz", "Nicht genug OzTile"), num5, num4);
		}
		else
		{
			statusText.text = T("Кузница ждёт выбранный камень.", "The Forge awaits a chosen stone.", "Dövme ocağı seçilen taşı bekliyor.", "Die Schmiede wartet auf einen ausgewaehlten Stein.");
		}
	}

	private void RefreshFurnaceTab(PlayerProfile profile, BattleTileStore store)
	{
		if (profile != null && !(store == null))
		{
			if (furnaceInfoText != null)
			{
				furnaceInfoText.text = T("Выберите вид возвышения и вручную отметьте резервные камни. Улучшенные камни повышают шанс успеха. Без подтверждения ничего не будет списано.", "Choose an ascension type and manually mark reserve stones. Upgraded stones increase the success chance. Nothing is consumed without confirmation.", "Yükseltme türünü seçin ve yedek taşları elle işaretleyin. Yükseltilmiş taşlar başarı şansını artırır. Onay olmadan hiçbir şey harcanmaz.", "Wähle eine Aufstiegsart und markiere Reservesteine manuell. Verbesserte Steine erhöhen die Erfolgschance. Ohne Bestätigung wird nichts verbraucht.");
			}
			RefreshAscendButtons(profile, store);
			statusText.text = string.Empty;
		}
	}

	private void SelectTile(string tileId, int upgradeLevel)
	{
		selectedTileId = tileId ?? string.Empty;
		selectedTileLevel = Mathf.Max(0, upgradeLevel);
		RefreshWindow();
		if (tutorialForgeActive)
		{
			SetTutorialStatus();
		}
	}

	private void ForgeSelectedTile()
	{
		if (forgeAnimationPlaying)
		{
			return;
		}
		PlayerProfile profile = GetProfile();
		BattleTileStore battleTileStore = ((BattleTileStore.I != null) ? BattleTileStore.I : UnityEngine.Object.FindAnyObjectByType<BattleTileStore>());
		bool flag = IsActiveTutorialForgeTarget();
		BattleTileData data = null;
		if (battleTileStore != null && !string.IsNullOrWhiteSpace(selectedTileId))
		{
			battleTileStore.TryGetTileDataById(selectedTileId, out data);
		}
		if (BattleTileInventoryService.TryForgeTile(profile, battleTileStore, selectedTileId, selectedTileLevel, out var newUpgradeLevel, out var remainingCopies, out var reason, flag))
		{
			ProfileService.I?.Save();
			bool showTutorialResult = flag && BattleLoreTutorialSession.IsActive && BattleLoreTutorialSession.ActiveStage == 4;
			StartCoroutine(PlayForgeCompleteRoutine(data, selectedTileId, newUpgradeLevel, remainingCopies, showTutorialResult));
			return;
		}
		else
		{
			RefreshWindow();
			statusText.text = ResolveForgeReason(reason);
			if (tutorialForgeActive)
			{
				SetTutorialStatus();
			}
		}
	}

	private IEnumerator PlayForgeCompleteRoutine(BattleTileData data, string tileId, int newLevel, int remainingCopies, bool showTutorialResult)
	{
		forgeAnimationPlaying = true;
		if (forgeButton != null)
		{
			forgeButton.interactable = false;
		}
		if (statusText != null)
		{
			statusText.text = T("Камни сходятся...", "Stones are merging...", "Taşlar birleşiyor...", "Steine verbinden sich...");
		}
		SetForgeSlotContentsVisible(visible: false);
		Transform effectParent = ((forgeSlotRoot != null) ? forgeSlotRoot : ((overlayRoot != null) ? overlayRoot.transform : base.transform));
		Sprite sprite = ((data?.Prefab != null) ? data.Prefab.FaceSprite : null);
		Vector2[] startPositions = new Vector2[3]
		{
			new Vector2(-230f, 116f),
			new Vector2(0f, 116f),
			new Vector2(230f, 116f)
		};
		Vector2 targetPosition = new Vector2(0f, 116f);
		RectTransform[] stones = new RectTransform[startPositions.Length];
		for (int i = 0; i < stones.Length; i++)
		{
			Image image = CreateImage(effectParent, "ForgeFlyStone" + i, startPositions[i], new Vector2(132f, 166f));
			image.sprite = sprite;
			image.enabled = sprite != null;
			image.color = new Color(1f, 1f, 1f, 1f);
			stones[i] = image.transform as RectTransform;
			if (stones[i] != null)
			{
				stones[i].SetAsLastSibling();
			}
		}
		float duration = 0.55f;
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float num = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
			for (int j = 0; j < stones.Length; j++)
			{
				if (!(stones[j] == null))
				{
					stones[j].anchoredPosition = Vector2.Lerp(startPositions[j], targetPosition, num) + new Vector2(0f, Mathf.Sin(num * MathF.PI) * 34f);
					float num2 = Mathf.Lerp(1f, 0.78f, num);
					stones[j].localScale = new Vector3(num2, num2, 1f);
				}
			}
			yield return null;
		}
		PlayForgeHammerSequence();
		Image heatGlow = CreateImage(effectParent, "ForgeHeatGlow", targetPosition, new Vector2(360f, 360f));
		heatGlow.sprite = ResolveForgeGlowSprite();
		heatGlow.color = new Color(1f, 0.38f, 0.06f, 0f);
		heatGlow.raycastTarget = false;
		RectTransform heatGlowRect = heatGlow.transform as RectTransform;
		if (heatGlowRect != null)
		{
			heatGlowRect.SetAsLastSibling();
		}
		Image heatRing = CreateImage(effectParent, "ForgeHeatRing", targetPosition, new Vector2(520f, 520f));
		heatRing.sprite = ResolveForgeGlowSprite();
		heatRing.color = new Color(1f, 0.82f, 0.24f, 0f);
		heatRing.raycastTarget = false;
		RectTransform heatRingRect = heatRing.transform as RectTransform;
		if (heatRingRect != null)
		{
			heatRingRect.SetAsLastSibling();
		}
		Image fusedStone = CreateImage(effectParent, "ForgeFusedStonePreview", targetPosition, new Vector2(146f, 184f));
		fusedStone.sprite = sprite;
		fusedStone.enabled = sprite != null;
		fusedStone.color = new Color(1f, 0.58f, 0.22f, 0f);
		fusedStone.raycastTarget = false;
		RectTransform fusedStoneRect = fusedStone.transform as RectTransform;
		if (fusedStoneRect != null)
		{
			fusedStoneRect.SetAsLastSibling();
		}
		elapsed = 0f;
		duration = 0.62f;
		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float num3 = Mathf.Clamp01(elapsed / duration);
			float num4 = Mathf.Sin(num3 * MathF.PI);
			heatGlow.color = new Color(1f, Mathf.Lerp(0.22f, 0.64f, num4), 0.04f, num4 * 0.84f);
			heatRing.color = new Color(1f, 0.82f, 0.22f, num4 * 0.44f);
			if (fusedStone != null)
			{
				fusedStone.color = Color.Lerp(new Color(1f, 0.55f, 0.18f, num4), Color.white, Mathf.Clamp01(num3 * 1.35f));
			}
			if (heatGlowRect != null)
			{
				float num5 = Mathf.Lerp(0.62f, 1.18f, num4);
				heatGlowRect.localScale = new Vector3(num5, num5, 1f);
				heatGlowRect.localRotation = Quaternion.Euler(0f, 0f, num3 * 18f);
			}
			if (heatRingRect != null)
			{
				float num6 = Mathf.Lerp(0.24f, 1.12f, num3);
				heatRingRect.localScale = new Vector3(num6, num6, 1f);
				heatRingRect.localRotation = Quaternion.Euler(0f, 0f, (0f - num3) * 24f);
			}
			if (fusedStoneRect != null)
			{
				float num7 = Mathf.Lerp(0.76f, 1.08f, num4);
				fusedStoneRect.localScale = new Vector3(num7, num7, 1f);
			}
			yield return null;
		}
		if (fusedStone != null)
		{
			fusedStone.color = Color.white;
		}
		yield return new WaitForSecondsRealtime(0.18f);
		for (int k = 0; k < stones.Length; k++)
		{
			if (stones[k] != null)
			{
				UnityEngine.Object.Destroy(stones[k].gameObject);
			}
		}
		if (heatGlow != null)
		{
			UnityEngine.Object.Destroy(heatGlow.gameObject);
		}
		if (heatRing != null)
		{
			UnityEngine.Object.Destroy(heatRing.gameObject);
		}
		if (fusedStone != null)
		{
			UnityEngine.Object.Destroy(fusedStone.gameObject);
		}
		forgeAnimationPlaying = false;
		selectedTileLevel = Mathf.Max(0, newLevel);
		ProfileService.I?.NotifyProfileChanged();
		RefreshWindow();
		if (statusText != null)
		{
			statusText.text = string.Empty;
		}
		ShowForgeResult(data, tileId, newLevel, showTutorialResult);
	}

	private void SetForgeSlotContentsVisible(bool visible)
	{
		if (forgeSlotRoot == null)
		{
			return;
		}
		for (int i = 0; i < 3; i++)
		{
			Transform transform = forgeSlotRoot.Find("ForgeSlot" + i);
			if (!(transform == null))
			{
				SetChildGraphicVisible(transform, "Face", visible);
				SetChildGraphicVisible(transform, "SlotText", visible);
				SetChildObjectVisible(transform, "UpgradeStars", visible);
			}
		}
	}

	private static void SetChildGraphicVisible(Transform parent, string childName, bool visible)
	{
		Transform transform = ((parent != null) ? parent.Find(childName) : null);
		if (!(transform == null))
		{
			Graphic component = transform.GetComponent<Graphic>();
			if (component != null)
			{
				component.enabled = visible;
			}
		}
	}

	private static void SetChildObjectVisible(Transform parent, string childName, bool visible)
	{
		Transform transform = ((parent != null) ? parent.Find(childName) : null);
		if (transform != null)
		{
			transform.gameObject.SetActive(visible);
		}
	}

	private void ShowForgeResult(BattleTileData data, string tileId, int newLevel, bool continueTutorial)
	{
		if (overlayRoot == null)
		{
			return;
		}
		if (tutorialResultRoot != null)
		{
			UnityEngine.Object.Destroy(tutorialResultRoot);
		}
		tutorialResultRoot = new GameObject("ForgeResultOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		tutorialResultRoot.transform.SetParent(overlayRoot.transform, worldPositionStays: false);
		RectTransform component = tutorialResultRoot.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		Image component2 = tutorialResultRoot.GetComponent<Image>();
		component2.color = Color.black;
		component2.raycastTarget = true;
		GameObject gameObject = CreatePanel(tutorialResultRoot.transform, "ResultPanel", new Vector2(2200f, 920f), Vector2.zero);
		ApplyForgeResultTextGlow(CreateText(gameObject.transform, "Title", T("Камень улучшен", "Stone Upgraded", "Taş Güçlendi", "Stein verbessert"), new Vector2(0f, 382f), new Vector2(1500f, 68f), 58f, TextAlignmentOptions.Center), new Color(1f, 0.78f, 0.42f, 1f), 0.18f);
		Image image = CreateImage(gameObject.transform, "StoneFace", new Vector2(-850f, 8f), new Vector2(320f, 410f));
		image.sprite = ((data?.Prefab != null) ? data.Prefab.FaceSprite : null);
		image.enabled = image.sprite != null;
		BattleTileUpgradeVisual.Apply(gameObject.transform, new Vector2(-850f, 8f), image.rectTransform.sizeDelta, Mathf.Max(0, newLevel), image.enabled);
		ApplyForgeResultTextGlow(CreateText(gameObject.transform, "StoneName", ResolveTileName(data, tileId) + "  +" + Mathf.Max(0, newLevel), new Vector2(220f, 292f), new Vector2(1520f, 62f), 48f, TextAlignmentOptions.Center), new Color(1f, 0.82f, 0.48f, 1f), 0.16f);
		GetUpgradePowerMultipliers(data, Mathf.Max(0, newLevel), out float previousPower, out float nextPower);
		TMP_Text powerText = CreateText(gameObject.transform, "StonePower", T("Сила камня", "Stone Power", "Taş Gücü", "Steinkraft") + $": x{nextPower:0.00}  <color=#74E8A5>(+{nextPower - previousPower:0.00})</color>", new Vector2(220f, 236f), new Vector2(1380f, 46f), 32f, TextAlignmentOptions.Center);
		powerText.color = new Color(1f, 0.93f, 0.78f, 1f);
		TMP_Text statsTitle = CreateText(gameObject.transform, "StatsTitle", T("Новые характеристики", "New Attributes", "Yeni Özellikler", "Neue Werte"), new Vector2(220f, 188f), new Vector2(1380f, 42f), 32f, TextAlignmentOptions.Center);
		statsTitle.color = new Color(1f, 0.72f, 0.3f, 1f);
		CreateUpgradeStatColumn(gameObject.transform, "PassiveStats", T("Пассивный эффект", "Passive Effect", "Pasif Etki", "Passiver Effekt"), BuildBonusUpgradeSummary(data?.PassiveBonus, previousPower, nextPower), new Vector2(-310f, -58f));
		CreateUpgradeStatColumn(gameObject.transform, "ActiveStats", T("Активный эффект", "Active Effect", "Aktif Etki", "Aktiver Effekt"), BuildActiveBonusUpgradeSummary(data?.ActiveBonus, previousPower, nextPower), new Vector2(230f, -58f));
		CreateUpgradeStatColumn(gameObject.transform, "SymbiosisStats", T("Симбиоз", "Symbiosis", "Simbiyoz", "Symbiose"), BuildBonusUpgradeSummary(data?.SymbiosisBonus, previousPower, nextPower), new Vector2(770f, -58f));
		CreateUpgradeColumnDivider(gameObject.transform, "DividerLeft", -40f);
		CreateUpgradeColumnDivider(gameObject.transform, "DividerRight", 500f);
		PlayVictorySound();
		CreateButton(gameObject.transform, "ButtonCloseForgeResult", T("Готово", "Done", "Tamam", "Fertig"), new Vector2(0f, -396f), new Vector2(440f, 86f), delegate
		{
			if (tutorialResultRoot != null)
			{
				UnityEngine.Object.Destroy(tutorialResultRoot);
				tutorialResultRoot = null;
			}
			if (continueTutorial)
			{
				ShowTutorialForgeRewardResult();
			}
			else
			{
				RefreshWindow();
			}
		});
	}

	private void CreateUpgradeStatColumn(Transform parent, string objectName, string title, string values, Vector2 position)
	{
		GameObject column = new GameObject(objectName, typeof(RectTransform));
		column.transform.SetParent(parent, worldPositionStays: false);
		RectTransform rect = column.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = position;
		rect.sizeDelta = new Vector2(480f, 390f);
		TMP_Text header = CreateText(column.transform, "Header", title, new Vector2(0f, 148f), new Vector2(450f, 54f), 36f, TextAlignmentOptions.Center);
		header.color = new Color(1f, 0.72f, 0.3f, 1f);
		string displayValues = string.IsNullOrWhiteSpace(values) ? T("Нет дополнительного эффекта", "No additional effect", "Ek etki yok", "Kein zusätzlicher Effekt") : values;
		TMP_Text body = CreateText(column.transform, "Values", displayValues, new Vector2(0f, -28f), new Vector2(430f, 280f), 32f, TextAlignmentOptions.TopLeft);
		body.color = new Color(1f, 0.93f, 0.78f, 1f);
	}

	private void CreateUpgradeColumnDivider(Transform parent, string objectName, float x)
	{
		Image divider = CreateImage(parent, objectName, new Vector2(x, -58f), new Vector2(3f, 310f));
		divider.preserveAspect = false;
		divider.color = new Color(0.74f, 0.47f, 0.16f, 0.58f);
	}

	private void ShowTutorialForgeRewardResult()
	{
		if (overlayRoot == null)
		{
			return;
		}
		if (tutorialResultRoot != null)
		{
			UnityEngine.Object.Destroy(tutorialResultRoot);
		}
		BattleLoreTutorialSession.GrantStageReward(4);
		BattleLoreTutorialSession.CompleteActiveStage();
		BattleLoreTutorialSession.ClearActive();
		tutorialForgeActive = false;
		tutorialForgeTileId = string.Empty;
		tutorialResultRoot = new GameObject("ForgeTutorialRewardOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		tutorialResultRoot.transform.SetParent(overlayRoot.transform, worldPositionStays: false);
		RectTransform component = tutorialResultRoot.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		Image component2 = tutorialResultRoot.GetComponent<Image>();
		component2.color = Color.black;
		component2.raycastTarget = true;
		PlayVictorySound();
		int stageOzTileReward = BattleLoreTutorialSession.GetStageOzTileReward(4);
		GameObject gameObject = CreatePanel(tutorialResultRoot.transform, "RewardPanel", new Vector2(1120f, 620f), Vector2.zero);
		CreateText(gameObject.transform, "Title", T("Урок завершен", "Lesson Complete", "Ders Tamamlandı", "Lektion abgeschlossen"), new Vector2(0f, 214f), new Vector2(860f, 70f), 54f, TextAlignmentOptions.Center);
		CreateText(gameObject.transform, "Body", T("Кузница готова. Три одинаковых редких камня или выше теперь можно соединять в усиленный камень.", "Forge is ready. Three identical rare+ stones can now become one upgraded stone.", "Forge hazır. Üç aynı rare+ taş artık tek güçlendirilmiş taşa dönüşebilir.", "Forge ist bereit. Drei gleiche Rare+ Steine koennen jetzt zu einem verbesserten Stein werden."), new Vector2(0f, 56f), new Vector2(860f, 140f), 34f, TextAlignmentOptions.Center);
		CreateText(gameObject.transform, "Reward", T("Награда", "Reward", "Ödül", "Belohnung") + $": +{stageOzTileReward} OzTile", new Vector2(0f, -92f), new Vector2(760f, 70f), 42f, TextAlignmentOptions.Center);
		CreateButton(gameObject.transform, "ButtonCloseReward", T("Забрать", "Claim", "Al", "Nehmen"), new Vector2(0f, -232f), new Vector2(420f, 90f), delegate
		{
			if (tutorialResultRoot != null)
			{
				UnityEngine.Object.Destroy(tutorialResultRoot);
				tutorialResultRoot = null;
			}
			if (overlayRoot != null)
			{
				overlayRoot.SetActive(value: false);
			}
			BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.Forge);
			BattleLoreTutorialUI.TryOpenWindowFromLobby();
		});
	}

	private bool IsActiveTutorialForgeTarget()
	{
		if (tutorialForgeActive && BattleLoreTutorialSession.IsActive && BattleLoreTutorialSession.ActiveStage == 4 && !string.IsNullOrWhiteSpace(tutorialForgeTileId))
		{
			return selectedTileLevel == 0 && string.Equals(selectedTileId, tutorialForgeTileId, StringComparison.Ordinal);
		}
		return false;
	}

	private void SetTutorialStatus()
	{
		if (!(statusText == null) && tutorialForgeActive)
		{
			statusText.text = (selectedTileLevel == 0 && string.Equals(selectedTileId, tutorialForgeTileId, StringComparison.Ordinal) ? T("Урок кузницы: нажми «Соединить». Это учебное слияние не тратит OzTile.", "Forge lesson: press Combine. This training combine does not spend OzTile.", "Forge dersi: Birleştir'e bas. Bu eğitim birleştirmesi OzTile harcamaz.", "Forge-Lektion: Druecke Verbinden. Diese Uebung kostet kein OzTile.") : T("Урок кузницы: выбери подготовленный редкий камень +0 с 3 копиями.", "Forge lesson: choose the prepared +0 Rare stone with 3 copies.", "Forge dersi: 3 kopyalı hazırlanmış +0 rare taşı seç.", "Forge-Lektion: Waehle den vorbereiteten +0 Rare-Stein mit 3 Kopien."));
		}
	}

	private void OpenAscendSelection(BattleTileRarity targetRarity)
	{
		PlayerProfile profile = GetProfile();
		BattleTileStore store = ((BattleTileStore.I != null) ? BattleTileStore.I : UnityEngine.Object.FindAnyObjectByType<BattleTileStore>());
		if (overlayRoot == null || profile == null || store == null || !BattleTileInventoryService.TryGetForgeAscendRequirements(targetRarity, out BattleTileRarity sourceRarity, out int requiredCopies, out int ozTileCost, out float chance))
		{
			statusText.text = T("Возвышение сейчас недоступно.", "Ascension is currently unavailable.", "Yükseltme şu anda kullanılamıyor.", "Der Aufstieg ist derzeit nicht verfügbar.");
			return;
		}

		CloseAscendSelection();
		ascendSelectionTargetRarity = targetRarity;
		ascendSelectedSacrifices.Clear();
		ascendSelectionRoot = new GameObject("AscendSelectionOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		ascendSelectionRoot.transform.SetParent(overlayRoot.transform, worldPositionStays: false);
		RectTransform rootRect = ascendSelectionRoot.GetComponent<RectTransform>();
		rootRect.anchorMin = Vector2.zero;
		rootRect.anchorMax = Vector2.one;
		rootRect.offsetMin = Vector2.zero;
		rootRect.offsetMax = Vector2.zero;
		Image rootImage = ascendSelectionRoot.GetComponent<Image>();
		rootImage.color = new Color(0f, 0f, 0f, 0.9f);
		rootImage.raycastTarget = true;
		ascendSelectionRoot.transform.SetAsLastSibling();

		GameObject panel = CreatePanel(ascendSelectionRoot.transform, "SelectionPanel", new Vector2(1980f, 900f), Vector2.zero);
		RectTransform panelRect = panel.GetComponent<RectTransform>();
		panelRect.anchorMin = Vector2.zero;
		panelRect.anchorMax = Vector2.one;
		panelRect.offsetMin = new Vector2(18f, 18f);
		panelRect.offsetMax = new Vector2(-18f, -18f);
		ApplyForgeResultTextGlow(CreateText(panel.transform, "Title", T("Выберите камни для возвышения", "Choose Stones for Ascension", "Yükseltme Taşlarını Seç", "Steine für den Aufstieg wählen"), new Vector2(0f, 430f), new Vector2(1900f, 76f), 60f, TextAlignmentOptions.Center), new Color(1f, 0.8f, 0.4f, 1f), 0.14f);
		string description = string.Format(T("Перенесите {0} резервных камней из блока «Доступные» в «На переработку». Требуемая редкость: {1}. Возможная награда — камень редкости «{2}». Каждый уровень жертвы добавляет +2%, а неудача усиливает следующую попытку.", "Move {0} reserve stones from Available to Recycling. Required rarity: {1}. Possible reward: a {2} stone. Each sacrifice level adds +2%, and a failure strengthens the next attempt.", "{0} yedek taşı Kullanılabilir bölümünden Geri Dönüşüm bölümüne taşıyın. Gerekli nadirlik: {1}. Olası ödül: {2} taş. Her kurban seviyesi +%2 ekler; başarısızlık sonraki denemeyi güçlendirir.", "Verschiebe {0} Reservesteine von Verfügbar zu Verwertung. Benötigte Seltenheit: {1}. Mögliche Belohnung: ein Stein der Seltenheit {2}. Jede Opferstufe gibt +2 %, ein Fehlschlag stärkt den nächsten Versuch."), requiredCopies, ResolveRarityName(sourceRarity), ResolveRarityName(targetRarity));
		TMP_Text descriptionText = CreateText(panel.transform, "Description", description, new Vector2(0f, 334f), new Vector2(2100f, 92f), 32f, TextAlignmentOptions.Center);
		descriptionText.color = new Color(0.95f, 0.88f, 0.7f, 1f);
		CreateText(panel.transform, "AvailableTitle", T("Доступные", "Available", "Kullanılabilir", "Verfügbar"), new Vector2(-560f, 244f), new Vector2(820f, 54f), 42f, TextAlignmentOptions.Center).color = new Color(1f, 0.82f, 0.46f, 1f);
		CreateText(panel.transform, "RecyclingTitle", T("На переработку", "For Recycling", "Geri Dönüşüme", "Zur Verwertung"), new Vector2(560f, 244f), new Vector2(820f, 54f), 42f, TextAlignmentOptions.Center).color = new Color(1f, 0.62f, 0.28f, 1f);
		GameObject availableBlock = CreatePanel(panel.transform, "AvailableBlock", new Vector2(1040f, 460f), new Vector2(-560f, -14f));
		ascendSelectionContent = CreateScrollContent(availableBlock.transform, "AvailableScroll", new Vector2(0f, 0f), new Vector2(920f, 390f));
		GameObject selectedBlock = CreatePanel(panel.transform, "RecyclingBlock", new Vector2(1040f, 460f), new Vector2(560f, -14f));
		ascendSelectedContent = CreateScrollContent(selectedBlock.transform, "SelectedScroll", new Vector2(0f, 0f), new Vector2(920f, 390f));
		ascendSelectionCounterText = CreateText(panel.transform, "SelectionCounter", string.Empty, new Vector2(-500f, -282f), new Vector2(420f, 50f), 36f, TextAlignmentOptions.Center);
		CreateText(panel.transform, "SelectionCostLabel", T("Стоимость", "Cost", "Bedel", "Kosten"), new Vector2(-160f, -282f), new Vector2(220f, 50f), 34f, TextAlignmentOptions.Right);
		Image selectionCurrencyIcon = CreateImage(panel.transform, "SelectionCurrencyIcon", new Vector2(-22f, -282f), new Vector2(38f, 38f));
		selectionCurrencyIcon.sprite = LoadOzTileIcon();
		selectionCurrencyIcon.enabled = selectionCurrencyIcon.sprite != null;
		ascendSelectionCostText = CreateText(panel.transform, "SelectionCost", string.Empty, new Vector2(180f, -282f), new Vector2(360f, 50f), 34f, TextAlignmentOptions.Left);
		ascendSelectionChanceText = CreateText(panel.transform, "SelectionChance", string.Empty, new Vector2(570f, -282f), new Vector2(520f, 50f), 32f, TextAlignmentOptions.Center);
		ascendSelectionFeedbackText = CreateText(panel.transform, "SelectionFeedback", T("Нажмите на камень слева, чтобы отправить одну копию на переработку. Нажмите справа, чтобы вернуть её.", "Tap a stone on the left to send one copy for recycling. Tap it on the right to return it.", "Bir kopyayı geri dönüşüme göndermek için soldaki taşa dokunun. Geri almak için sağdaki taşa dokunun.", "Tippe links auf einen Stein, um eine Kopie zu verwerten. Tippe rechts, um sie zurückzunehmen."), new Vector2(0f, -342f), new Vector2(1900f, 48f), 29f, TextAlignmentOptions.Center);
		ascendSelectionFeedbackText.color = new Color(0.9f, 0.72f, 0.46f, 1f);
		Button ascendCancelButton = CreateButton(panel.transform, "ButtonCancelAscension", T("Назад", "Back", "Geri", "Zurück"), new Vector2(-290f, -430f), new Vector2(470f, 92f), CloseAscendSelection);
		ascendSelectionConfirmButton = CreateButton(panel.transform, "ButtonConfirmAscension", T("Начать возвышение", "Begin Ascension", "Yükseltmeyi Başlat", "Aufstieg beginnen"), new Vector2(290f, -430f), new Vector2(560f, 92f), CommitAscendSelection);
		TMP_Text cancelLabel = ascendCancelButton.GetComponentInChildren<TMP_Text>(includeInactive: true);
		TMP_Text confirmLabel = ascendSelectionConfirmButton.GetComponentInChildren<TMP_Text>(includeInactive: true);
		if (cancelLabel != null)
			cancelLabel.fontSize = 40f;
		if (confirmLabel != null)
			confirmLabel.fontSize = 40f;
		RebuildAscendSelection();
	}

	private void RebuildAscendSelection()
	{
		if (ascendSelectionRoot == null || ascendSelectionContent == null || ascendSelectedContent == null)
		{
			return;
		}
		PlayerProfile profile = GetProfile();
		BattleTileStore store = ((BattleTileStore.I != null) ? BattleTileStore.I : UnityEngine.Object.FindAnyObjectByType<BattleTileStore>());
		if (profile == null || store == null || !BattleTileInventoryService.TryGetForgeAscendRequirements(ascendSelectionTargetRarity, out BattleTileRarity sourceRarity, out int requiredCopies, out int ozTileCost, out float chance))
		{
			return;
		}

		ClearDynamicChildren(ascendSelectionContent);
		ClearDynamicChildren(ascendSelectedContent);
		BattleTileInventoryService.EnsureInventoryForStore(profile, store);
		MahjongBattleTileInventoryData inventory = BattleTileInventoryService.GetOrCreateInventory(profile);
		int availableCardIndex = 0;
		if (inventory?.TileStacks != null)
		{
			List<MahjongBattleTileStackData> eligibleStacks = new List<MahjongBattleTileStackData>();
			for (int i = 0; i < inventory.TileStacks.Count; i++)
			{
				MahjongBattleTileStackData stack = inventory.TileStacks[i];
				if (stack == null || !store.TryGetTileDataById(stack.TileId, out BattleTileData data) || data == null || data.Rarity != sourceRarity || BattleTileInventoryService.IsBaseBattleTile(stack.TileId))
				{
					continue;
				}
				int upgradeLevel = Mathf.Max(0, stack.UpgradeLevel);
				int remainingCopies = BattleTileInventoryService.GetReserveCopyCount(inventory, stack.TileId, upgradeLevel) - CountSelectedAscendCopies(stack.TileId, upgradeLevel);
				if (remainingCopies > 0)
					eligibleStacks.Add(stack);
			}
			eligibleStacks.Sort((left, right) =>
			{
				int levelOrder = right.UpgradeLevel.CompareTo(left.UpgradeLevel);
				return levelOrder != 0 ? levelOrder : string.Compare(left.TileId, right.TileId, StringComparison.Ordinal);
			});

			for (int i = 0; i < eligibleStacks.Count; i++)
			{
				MahjongBattleTileStackData stack = eligibleStacks[i];
				store.TryGetTileDataById(stack.TileId, out BattleTileData data);
				string id = stack.TileId.Trim();
				int upgradeLevel = Mathf.Max(0, stack.UpgradeLevel);
				int selectableCopies = BattleTileInventoryService.GetReserveCopyCount(inventory, id, upgradeLevel);
				int selectedCopies = CountSelectedAscendCopies(id, upgradeLevel);
				int availableCopies = Mathf.Max(0, selectableCopies - selectedCopies);
				if (availableCopies > 0)
				{
					int column = availableCardIndex % 5;
					int row = availableCardIndex / 5;
					Button card = CreateStoneRow(ascendSelectionContent, "Available_" + availableCardIndex, data, id, availableCopies, upgradeLevel, selected: false, new Vector2(-360f + column * 180f, -6f - row * 190f), delegate
					{
						AddAscendSacrifice(id, upgradeLevel, requiredCopies);
					});
					card.transform.localScale = Vector3.one * 1.1f;
					card.interactable = !ascendProcessPlaying && ascendSelectedSacrifices.Count < requiredCopies;
					availableCardIndex++;
				}
			}
		}

		if (availableCardIndex == 0)
		{
			CreateText(ascendSelectionContent, "Empty", T("Нет доступных камней", "No available stones", "Kullanılabilir taş yok", "Keine verfügbaren Steine"), new Vector2(0f, -90f), new Vector2(600f, 90f), 31f, TextAlignmentOptions.Center);
		}

		RectTransform contentRect = ascendSelectionContent as RectTransform;
		if (contentRect != null)
		{
			int rows = Mathf.Max(1, Mathf.CeilToInt(availableCardIndex / 5f));
			contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, Mathf.Max(390f, rows * 190f + 10f));
		}

		for (int i = 0; i < ascendSelectedSacrifices.Count; i++)
		{
			int selectionIndex = i;
			BattleTileInventoryService.ForgeAscendSacrifice sacrifice = ascendSelectedSacrifices[i];
			string id = sacrifice.TileId;
			if (!store.TryGetTileDataById(id, out BattleTileData data) || data == null)
			{
				continue;
			}
			int column = selectionIndex % 5;
			int row = selectionIndex / 5;
			Button card = CreateStoneRow(ascendSelectedContent, "Recycling_" + selectionIndex, data, id, 1, sacrifice.UpgradeLevel, selected: true, new Vector2(-360f + column * 180f, -6f - row * 190f), delegate
			{
				RemoveAscendSacrificeAt(selectionIndex);
			});
			card.transform.localScale = Vector3.one * 1.1f;
			card.interactable = !ascendProcessPlaying;
			Transform countLabel = card.transform.Find("Count");
			if (countLabel != null)
			{
				countLabel.gameObject.SetActive(false);
			}
		}

		if (ascendSelectedSacrifices.Count == 0)
		{
			CreateText(ascendSelectedContent, "Empty", T("Перенесите сюда камни", "Move stones here", "Taşları buraya taşıyın", "Steine hierher verschieben"), new Vector2(0f, -90f), new Vector2(600f, 90f), 31f, TextAlignmentOptions.Center);
		}
		RectTransform selectedContentRect = ascendSelectedContent as RectTransform;
		if (selectedContentRect != null)
		{
			int rows = Mathf.Max(1, Mathf.CeilToInt(ascendSelectedSacrifices.Count / 5f));
			selectedContentRect.sizeDelta = new Vector2(selectedContentRect.sizeDelta.x, Mathf.Max(390f, rows * 190f + 10f));
		}

		int selectedCount = ascendSelectedSacrifices.Count;
		int ozTileBalance = CurrencyService.I != null ? CurrencyService.I.GetOzTile() : profile?.Currencies != null ? profile.Currencies.OzTile : 0;
		float effectiveChance = BattleTileInventoryService.GetForgeAscendPreviewChance(profile, ascendSelectionTargetRarity, ascendSelectedSacrifices, out float sacrificeChanceBonus, out float failureChanceBonus, out bool guaranteedChance);
		int effectiveChancePercent = Mathf.RoundToInt(effectiveChance * 100f);
		int sacrificeBonusPercent = Mathf.RoundToInt(sacrificeChanceBonus * 100f);
		int failureBonusPercent = Mathf.RoundToInt(failureChanceBonus * 100f);
		if (ascendSelectionCounterText != null)
		{
			ascendSelectionCounterText.text = string.Format(T("Выбрано: {0}/{1}", "Selected: {0}/{1}", "Seçildi: {0}/{1}", "Ausgewählt: {0}/{1}"), selectedCount, requiredCopies);
			ascendSelectionCounterText.color = selectedCount == requiredCopies ? new Color(0.42f, 1f, 0.62f, 1f) : Color.white;
		}
		if (ascendSelectionCostText != null)
		{
			ascendSelectionCostText.text = string.Format(T("{0}  (есть {1})", "{0}  (owned {1})", "{0}  (mevcut {1})", "{0}  (vorhanden {1})"), ozTileCost, ozTileBalance);
			ascendSelectionCostText.color = ozTileBalance >= ozTileCost ? Color.white : new Color(1f, 0.48f, 0.38f, 1f);
		}
		if (ascendSelectionChanceText != null)
		{
			if (guaranteedChance)
			{
				ascendSelectionChanceText.text = T("Шанс: 100% — гарантия", "Chance: 100% — guaranteed", "Şans: %100 — garantili", "Chance: 100 % — garantiert");
			}
			else if (sacrificeBonusPercent > 0 && failureBonusPercent > 0)
			{
				ascendSelectionChanceText.text = string.Format(T("Шанс: {0}%  (+{1}% камни, +{2}% серия)", "Chance: {0}%  (+{1}% stones, +{2}% streak)", "Şans: %{0}  (+%{1} taş, +%{2} seri)", "Chance: {0} %  (+{1} % Steine, +{2} % Serie)"), effectiveChancePercent, sacrificeBonusPercent, failureBonusPercent);
			}
			else if (sacrificeBonusPercent > 0 || failureBonusPercent > 0)
			{
				int visibleBonus = sacrificeBonusPercent + failureBonusPercent;
				ascendSelectionChanceText.text = string.Format(T("Шанс: {0}%  (+{1}%)", "Chance: {0}%  (+{1}%)", "Şans: %{0}  (+%{1})", "Chance: {0} %  (+{1} %)"), effectiveChancePercent, visibleBonus);
			}
			else
			{
				ascendSelectionChanceText.text = string.Format(T("Шанс: {0}%", "Chance: {0}%", "Şans: %{0}", "Chance: {0} %"), effectiveChancePercent);
			}
			ascendSelectionChanceText.color = guaranteedChance || sacrificeBonusPercent > 0 || failureBonusPercent > 0 ? new Color(0.48f, 1f, 0.62f, 1f) : Color.white;
		}
		if (ascendSelectionFeedbackText != null && !ascendProcessPlaying)
		{
			ascendSelectionFeedbackText.color = new Color(0.9f, 0.72f, 0.46f, 1f);
		}

		if (ascendSelectionConfirmButton != null)
		{
			bool canAscend = BattleTileInventoryService.CanForgeAscend(profile, store, ascendSelectionTargetRarity, ascendSelectedSacrifices, out string reason);
			ascendSelectionConfirmButton.interactable = !ascendProcessPlaying && canAscend;
			if (ascendSelectionCounterText != null)
			{
				ascendSelectionCounterText.color = canAscend ? new Color(0.42f, 1f, 0.62f, 1f) : selectedCount == requiredCopies ? new Color(1f, 0.5f, 0.4f, 1f) : Color.white;
			}
			if (!ascendProcessPlaying && selectedCount == requiredCopies && !canAscend && ascendSelectionFeedbackText != null)
			{
				StopAscendFeedbackHide();
				ascendSelectionFeedbackText.text = ResolveForgeReason(reason);
				ascendSelectionFeedbackText.color = new Color(1f, 0.48f, 0.38f, 1f);
			}
		}
	}

	private void AddAscendSacrifice(string tileId, int upgradeLevel, int requiredCopies)
	{
		if (ascendProcessPlaying || string.IsNullOrWhiteSpace(tileId))
		{
			return;
		}
		if (ascendSelectedSacrifices.Count >= requiredCopies)
		{
			return;
		}
		ascendSelectedSacrifices.Add(new BattleTileInventoryService.ForgeAscendSacrifice(tileId, upgradeLevel));
		ShowAscendTransientFeedback(T("Камень добавлен в переработку. До подтверждения его можно вернуть.", "Stone added for recycling. It can be returned before confirmation.", "Taş geri dönüşüme eklendi. Onaydan önce geri alınabilir.", "Stein zur Verwertung hinzugefügt. Vor der Bestätigung kann er zurückgenommen werden."));
		RebuildAscendSelection();
	}

	private void RemoveAscendSacrificeAt(int index)
	{
		if (ascendProcessPlaying || index < 0 || index >= ascendSelectedSacrifices.Count)
		{
			return;
		}
		ascendSelectedSacrifices.RemoveAt(index);
		ShowAscendTransientFeedback(T("Камень возвращён в доступные.", "Stone returned to Available.", "Taş Kullanılabilir bölümüne geri döndü.", "Stein zu Verfügbar zurückgegeben."));
		RebuildAscendSelection();
	}

	private int CountSelectedAscendCopies(string tileId, int upgradeLevel)
	{
		int count = 0;
		for (int i = 0; i < ascendSelectedSacrifices.Count; i++)
		{
			BattleTileInventoryService.ForgeAscendSacrifice sacrifice = ascendSelectedSacrifices[i];
			if (sacrifice != null && sacrifice.UpgradeLevel == Mathf.Max(0, upgradeLevel) && string.Equals(sacrifice.TileId, tileId, StringComparison.Ordinal))
			{
				count++;
			}
		}
		return count;
	}

	private void ShowAscendTransientFeedback(string message)
	{
		if (ascendSelectionFeedbackText == null)
		{
			return;
		}
		StopAscendFeedbackHide();
		ascendSelectionFeedbackText.text = message;
		ascendSelectionFeedbackText.color = new Color(0.9f, 0.72f, 0.46f, 1f);
		ascendFeedbackHideCoroutine = StartCoroutine(HideAscendFeedbackAfterDelay());
	}

	private IEnumerator HideAscendFeedbackAfterDelay()
	{
		yield return new WaitForSecondsRealtime(2.5f);
		if (ascendSelectionFeedbackText != null)
		{
			ascendSelectionFeedbackText.text = string.Empty;
		}
		ascendFeedbackHideCoroutine = null;
	}

	private void StopAscendFeedbackHide()
	{
		if (ascendFeedbackHideCoroutine != null)
		{
			StopCoroutine(ascendFeedbackHideCoroutine);
			ascendFeedbackHideCoroutine = null;
		}
	}

	private void CloseAscendSelection()
	{
		StopAscendFeedbackHide();
		if (ascendProcessCoroutine != null)
		{
			StopCoroutine(ascendProcessCoroutine);
			ascendProcessCoroutine = null;
		}
		if (ascendSelectionRoot != null)
		{
			UnityEngine.Object.Destroy(ascendSelectionRoot);
		}
		ascendSelectionRoot = null;
		ascendSelectionContent = null;
		ascendSelectedContent = null;
		ascendSelectionCounterText = null;
		ascendSelectionCostText = null;
		ascendSelectionChanceText = null;
		ascendSelectionFeedbackText = null;
		ascendSelectionConfirmButton = null;
		ascendSelectedSacrifices.Clear();
		ascendProcessPlaying = false;
	}

	private void CloseAscendResult()
	{
		if (ascendResultRoot != null)
		{
			UnityEngine.Object.Destroy(ascendResultRoot);
			ascendResultRoot = null;
		}
	}

	private void AscendForge(BattleTileRarity targetRarity)
	{
		OpenAscendSelection(targetRarity);
	}

	private void CommitAscendSelection()
	{
		if (ascendProcessPlaying)
		{
			return;
		}
		PlayerProfile profile = GetProfile();
		BattleTileStore store = ((BattleTileStore.I != null) ? BattleTileStore.I : UnityEngine.Object.FindAnyObjectByType<BattleTileStore>());
		if (!BattleTileInventoryService.CanForgeAscend(profile, store, ascendSelectionTargetRarity, ascendSelectedSacrifices, out string reason))
		{
			if (ascendSelectionFeedbackText != null)
			{
				ascendSelectionFeedbackText.text = ResolveForgeReason(reason);
			}
			RebuildAscendSelection();
			return;
		}
		List<BattleTileInventoryService.ForgeAscendSacrifice> selectedSacrifices = new List<BattleTileInventoryService.ForgeAscendSacrifice>(ascendSelectedSacrifices.Count);
		for (int i = 0; i < ascendSelectedSacrifices.Count; i++)
		{
			BattleTileInventoryService.ForgeAscendSacrifice sacrifice = ascendSelectedSacrifices[i];
			selectedSacrifices.Add(new BattleTileInventoryService.ForgeAscendSacrifice(sacrifice.TileId, sacrifice.UpgradeLevel));
		}
		ascendProcessCoroutine = StartCoroutine(PlayAscendProcessRoutine(profile, store, ascendSelectionTargetRarity, selectedSacrifices));
	}

	private IEnumerator PlayAscendProcessRoutine(PlayerProfile profile, BattleTileStore store, BattleTileRarity targetRarity, List<BattleTileInventoryService.ForgeAscendSacrifice> selectedSacrifices)
	{
		StopAscendFeedbackHide();
		ascendProcessPlaying = true;
		RebuildAscendSelection();
		if (ascendSelectionFeedbackText != null)
		{
			ascendSelectionFeedbackText.text = T("Кузница пробуждается…", "The Forge awakens…", "Ocak uyanıyor…", "Die Schmiede erwacht…");
		}

		GameObject fxRoot = new GameObject("AscendProcessFx", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		fxRoot.transform.SetParent(ascendSelectionRoot != null ? ascendSelectionRoot.transform : overlayRoot.transform, worldPositionStays: false);
		RectTransform fxRect = fxRoot.GetComponent<RectTransform>();
		fxRect.anchorMin = Vector2.zero;
		fxRect.anchorMax = Vector2.one;
		fxRect.offsetMin = Vector2.zero;
		fxRect.offsetMax = Vector2.zero;
		Image fxBackground = fxRoot.GetComponent<Image>();
		fxBackground.color = new Color(0.015f, 0.005f, 0f, 0.82f);
		fxBackground.raycastTarget = true;
		Image glow = CreateImage(fxRoot.transform, "ForgeGlow", Vector2.zero, new Vector2(620f, 620f));
		glow.sprite = ResolveForgeGlowSprite();
		glow.color = new Color(1f, 0.28f, 0.03f, 0.22f);
		Image ring = CreateImage(fxRoot.transform, "ForgeRing", Vector2.zero, new Vector2(390f, 390f));
		ring.sprite = ResolveForgeGlowSprite();
		ring.color = new Color(1f, 0.74f, 0.16f, 0.34f);

		int stoneCount = Mathf.Max(1, selectedSacrifices?.Count ?? 0);
		RectTransform[] stoneRects = new RectTransform[stoneCount];
		Vector2[] startPositions = new Vector2[stoneCount];
		for (int i = 0; i < stoneCount; i++)
		{
			float angle = Mathf.PI * 2f * i / stoneCount + Mathf.PI * 0.5f;
			Vector2 start = new Vector2(Mathf.Cos(angle) * 410f, Mathf.Sin(angle) * 250f);
			startPositions[i] = start;
			Image stone = CreateImage(fxRoot.transform, "SacrificeStone" + i, start, new Vector2(150f, 190f));
			if (selectedSacrifices != null && i < selectedSacrifices.Count && store.TryGetTileDataById(selectedSacrifices[i].TileId, out BattleTileData data))
			{
				stone.sprite = data?.Prefab != null ? data.Prefab.FaceSprite : null;
				BattleTileUpgradeVisual.Apply(stone.transform, Vector2.zero, new Vector2(150f, 190f), selectedSacrifices[i].UpgradeLevel);
			}
			stone.enabled = stone.sprite != null;
			stone.color = Color.white;
			stoneRects[i] = stone.rectTransform;
		}

		PlayForgeHammerSequence();
		bool secondHitPlayed = false;
		float duration = 1.45f;
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float progress = Mathf.Clamp01(elapsed / duration);
			float eased = Mathf.SmoothStep(0f, 1f, progress);
			for (int i = 0; i < stoneRects.Length; i++)
			{
				RectTransform stoneRect = stoneRects[i];
				if (stoneRect == null)
				{
					continue;
				}
				stoneRect.anchoredPosition = Vector2.Lerp(startPositions[i], Vector2.zero, eased);
				float scale = Mathf.Lerp(1f, 0.16f, eased);
				stoneRect.localScale = new Vector3(scale, scale, 1f);
				stoneRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 28f * (i % 2 == 0 ? 1f : -1f), eased));
			}
			float pulse = 0.78f + Mathf.Sin(progress * Mathf.PI * 7f) * 0.16f;
			glow.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
			ring.rectTransform.localRotation = Quaternion.Euler(0f, 0f, progress * 150f);
			if (!secondHitPlayed && progress >= 0.62f)
			{
				secondHitPlayed = true;
				PlayForgeHammerSequence();
			}
			yield return null;
		}

		BattleTileInventoryService.ForgeAscendResult result = BattleTileInventoryService.TryForgeAscend(profile, store, targetRarity, selectedSacrifices);
		if (fxRoot != null)
		{
			UnityEngine.Object.Destroy(fxRoot);
		}
		if (result == null || !result.Success)
		{
			ascendProcessCoroutine = null;
			ascendProcessPlaying = false;
			if (ascendSelectionFeedbackText != null)
			{
				ascendSelectionFeedbackText.text = ResolveForgeReason(result != null ? result.Message : string.Empty);
			}
			RebuildAscendSelection();
			yield break;
		}

		ascendProcessCoroutine = null;
		CloseAscendSelection();
		ProfileService.I?.NotifyProfileChanged();
		RefreshWindow();
		statusText.text = string.Empty;
		ShowAscendResult(result);
	}

	private void ShowAscendResult(BattleTileInventoryService.ForgeAscendResult result)
	{
		if (overlayRoot == null || result == null)
		{
			return;
		}
		if (ascendResultRoot != null)
		{
			UnityEngine.Object.Destroy(ascendResultRoot);
		}
		ascendResultRoot = new GameObject("AscendResultOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		ascendResultRoot.transform.SetParent(overlayRoot.transform, worldPositionStays: false);
		RectTransform rootRect = ascendResultRoot.GetComponent<RectTransform>();
		rootRect.anchorMin = Vector2.zero;
		rootRect.anchorMax = Vector2.one;
		rootRect.offsetMin = Vector2.zero;
		rootRect.offsetMax = Vector2.zero;
		Image rootImage = ascendResultRoot.GetComponent<Image>();
		rootImage.color = Color.black;
		rootImage.raycastTarget = true;
		ascendResultRoot.transform.SetAsLastSibling();

		GameObject panel = CreatePanel(ascendResultRoot.transform, "AscendResultPanel", new Vector2(1600f, 820f), Vector2.zero);
		string title = result.Hit ? T("Возвышение свершилось", "Ascension Complete", "Yükseltme Tamamlandı", "Aufstieg vollendet") : T("Пламя угасло", "The Flame Faded", "Alev Söndü", "Die Flamme erlosch");
		ApplyForgeResultTextGlow(CreateText(panel.transform, "Title", title, new Vector2(0f, 314f), new Vector2(1200f, 70f), 56f, TextAlignmentOptions.Center), result.Hit ? new Color(1f, 0.82f, 0.4f, 1f) : new Color(0.92f, 0.62f, 0.38f, 1f), 0.16f);

		if (result.Hit && result.RewardTile != null)
		{
			Image reward = CreateImage(panel.transform, "RewardStone", new Vector2(-330f, 28f), new Vector2(300f, 380f));
			reward.sprite = result.RewardTile.Prefab != null ? result.RewardTile.Prefab.FaceSprite : null;
			reward.enabled = reward.sprite != null;
			if (!reward.enabled)
			{
				CreateText(panel.transform, "RewardFallback", "?", new Vector2(-330f, 28f), new Vector2(180f, 180f), 92f, TextAlignmentOptions.Center);
			}
			string rewardName = ResolveTileName(result.RewardTile, result.RewardTile.Id);
			ApplyForgeResultTextGlow(CreateText(panel.transform, "RewardName", rewardName, new Vector2(300f, 112f), new Vector2(720f, 74f), 46f, TextAlignmentOptions.Center), new Color(1f, 0.84f, 0.5f, 1f), 0.12f);
			CreateText(panel.transform, "RewardRarity", ResolveRarityName(result.TargetRarity), new Vector2(300f, 42f), new Vector2(600f, 48f), 34f, TextAlignmentOptions.Center).color = ResolveForgeSlotOutlineColor(result.TargetRarity);
			string body = result.Pity ? T("Гарантия пробудила камень. Полученная копия добавлена в вашу коллекцию.", "The guarantee awakened a stone. The obtained copy was added to your collection.", "Garanti bir taşı uyandırdı. Kazanılan kopya koleksiyonunuza eklendi.", "Die Garantie erweckte einen Stein. Die erhaltene Kopie wurde deiner Sammlung hinzugefügt.") : T("Камень пробудился. Полученная копия добавлена в вашу коллекцию.", "A stone awakened. The obtained copy was added to your collection.", "Bir taş uyandı. Kazanılan kopya koleksiyonunuza eklendi.", "Ein Stein erwachte. Die erhaltene Kopie wurde deiner Sammlung hinzugefügt.");
			CreateText(panel.transform, "Body", body, new Vector2(300f, -78f), new Vector2(720f, 120f), 32f, TextAlignmentOptions.Center);
			PlayVictorySound();
		}
		else
		{
			int attemptsToGuarantee = Mathf.Max(1, result.PityLimit - result.PityCount);
			CreateText(panel.transform, "Body", T("Выбранные камни и OzTile были принесены в жертву, но новый камень не появился.", "The selected stones and OzTile were sacrificed, but no new stone appeared.", "Seçilen taşlar ve OzTile feda edildi ancak yeni taş ortaya çıkmadı.", "Die ausgewählten Steine und OzTile wurden geopfert, doch kein neuer Stein erschien."), new Vector2(0f, 72f), new Vector2(1100f, 130f), 36f, TextAlignmentOptions.Center);
			CreateText(panel.transform, "Guarantee", string.Format(T("До гарантированного результата осталось попыток: {0}", "Attempts remaining until a guaranteed result: {0}", "Garantili sonuca kalan deneme: {0}", "Versuche bis zum garantierten Ergebnis: {0}"), attemptsToGuarantee), new Vector2(0f, -72f), new Vector2(900f, 70f), 34f, TextAlignmentOptions.Center).color = new Color(1f, 0.76f, 0.38f, 1f);
		}

		CreateButton(panel.transform, "ButtonCloseAscendResult", T("Готово", "Done", "Tamam", "Fertig"), new Vector2(0f, -322f), new Vector2(420f, 86f), delegate
		{
			CloseAscendResult();
			RefreshWindow();
		});
	}

	private void RefreshAscendButtons(PlayerProfile profile, BattleTileStore store)
	{
		ConfigureAscendButton(ascendLegendaryButton, BattleTileRarity.Legendary, T("Легендарное возвышение", "Legendary Ascension", "Efsanevi Yükseltme", "Legendärer Aufstieg"), profile, store);
		ConfigureAscendButton(ascendMythicButton, BattleTileRarity.Mythic, T("Мифическое возвышение", "Mythic Ascension", "Mitik Yükseltme", "Mythischer Aufstieg"), profile, store);
	}

	private void ConfigureAscendButton(Button button, BattleTileRarity targetRarity, string title, PlayerProfile profile, BattleTileStore store)
	{
		if (!(button == null))
		{
			bool configured = BattleTileInventoryService.TryGetForgeAscendRequirements(targetRarity, out BattleTileRarity sourceRarity, out int requiredCopies, out _, out _);
			button.interactable = configured && profile != null && store != null;
			TMP_Text componentInChildren = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
			if (componentInChildren != null)
			{
				componentInChildren.text = configured ? title + "\n" + BuildAscendRequirementLabel(sourceRarity, requiredCopies) : title;
			}
		}
	}

	private string ResolveForgeReason(string reason)
	{
		if (string.IsNullOrWhiteSpace(reason))
		{
			return T("Кузница сейчас недоступна.", "Forge is not possible now.", "Forge simdi olmaz.", "Forge ist jetzt nicht moeglich.");
		}
		if (reason.IndexOf("Select exactly", StringComparison.OrdinalIgnoreCase) >= 0 || reason.IndexOf("selection required", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return T("Отметьте необходимое количество камней.", "Mark the required number of stones.", "Gerekli sayıda taşı işaretleyin.", "Markiere die erforderliche Anzahl an Steinen.");
		}
		if (reason.IndexOf("reserve copies", StringComparison.OrdinalIgnoreCase) >= 0 || reason.IndexOf("Invalid sacrifice", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return T("Один из выбранных камней больше недоступен. Выберите камни заново.", "One of the selected stones is no longer available. Choose the stones again.", "Seçilen taşlardan biri artık kullanılamıyor. Taşları yeniden seçin.", "Einer der ausgewählten Steine ist nicht mehr verfügbar. Wähle die Steine erneut.");
		}
		if (reason.IndexOf("Profile changed", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return T("Профиль изменился. Откройте выбор камней заново.", "The profile changed. Open stone selection again.", "Profil değişti. Taş seçimini yeniden açın.", "Das Profil wurde geändert. Öffne die Steinauswahl erneut.");
		}
		if (reason.IndexOf("No ", StringComparison.OrdinalIgnoreCase) >= 0 && reason.IndexOf("rewards configured", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return T("Для этого возвышения пока не настроена награда.", "No reward is configured for this ascension yet.", "Bu yükseltme için henüz ödül ayarlanmadı.", "Für diesen Aufstieg ist noch keine Belohnung eingerichtet.");
		}
		if (reason.IndexOf("Need", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			if (reason.IndexOf("OzTile", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return T("Не хватает OzTile для кузницы.", "Not enough OzTile for Forge.", "Forge için OzTile yetersiz.", "Nicht genug OzTile fuer Forge.");
			}
			return T("Не хватает свободных камней подходящей редкости.", "Not enough free stones of the required rarity.", "Gerekli nadirlikte yeterli boş taş yok.", "Nicht genügend freie Steine der benötigten Seltenheit.");
		}
		return reason;
	}

	private static string ResolveRarityName(BattleTileRarity rarity)
	{
		return rarity switch
		{
			BattleTileRarity.Mythic => T("Мифическая", "Mythic", "Mitik", "Mythisch"),
			BattleTileRarity.Legendary => T("Легендарная", "Legendary", "Efsanevi", "Legendär"),
			BattleTileRarity.Epic => T("Эпическая", "Epic", "Destansı", "Episch"),
			BattleTileRarity.Rare => T("Редкая", "Rare", "Nadir", "Selten"),
			BattleTileRarity.Common => T("Обычная", "Common", "Yaygın", "Gewöhnlich"),
			_ => T("Стандартная", "Standard", "Standart", "Standard"),
		};
	}

	private static string BuildAscendRequirementLabel(BattleTileRarity rarity, int count)
	{
		return rarity switch
		{
			BattleTileRarity.Legendary => string.Format(T("Выбрать {0} легендарных камня", "Choose {0} Legendary stones", "{0} Efsanevi taş seç", "{0} legendäre Steine wählen"), count),
			BattleTileRarity.Epic => string.Format(T("Выбрать {0} эпических камней", "Choose {0} Epic stones", "{0} Destansı taş seç", "{0} epische Steine wählen"), count),
			BattleTileRarity.Rare => string.Format(T("Выбрать {0} редких камней", "Choose {0} Rare stones", "{0} Nadir taş seç", "{0} seltene Steine wählen"), count),
			_ => string.Format(T("Выбрать {0} камней", "Choose {0} stones", "{0} taş seç", "{0} Steine wählen"), count),
		};
	}

	private static string FormatForgeResult(BattleTileData data, string tileId, int level, int ownedCount)
	{
		int num = level < int.MaxValue ? level + 1 : int.MaxValue;
		float num2 = BattleTileInventoryService.GetRarityPowerMultiplier(data.Rarity) * (1f + (float)level * 0.1f);
		float num3 = BattleTileInventoryService.GetRarityPowerMultiplier(data.Rarity) * (1f + (float)num * 0.1f);
		int remainingCopies = Mathf.Max(0, ownedCount - 2);
		int missingCopies = Mathf.Max(0, 3 - ownedCount);
		string copyResult = ownedCount >= 3 ? T("После закалки останется", "Remaining after forging", "Dövmeden sonra kalacak", "Nach dem Schmieden uebrig") + $": {remainingCopies}" : T("Нужно найти ещё", "Still needed", "Daha gerekli", "Noch benoetigt") + $": {missingCopies}";
		return "<color=#F5CB72>" + ResolveTileName(data, tileId) + "</color>\n" + T("Закалка", "Forging", "Dövme", "Schmieden") + $": +{level} → +{num}   |   " + T("Сила", "Power", "Güç", "Kraft") + $": x{num2:0.00} → x{num3:0.00}\n" + T("Собрано копий", "Copies collected", "Toplanan kopya", "Gesammelte Kopien") + $": {ownedCount}/3   |   " + copyResult;
	}

	private static Color ResolveForgeSlotColor(BattleTileRarity rarity)
	{
		return rarity switch
		{
			BattleTileRarity.Mythic => new Color(0.28f, 0.055f, 0.09f, 0.96f),
			BattleTileRarity.Legendary => new Color(0.3f, 0.19f, 0.035f, 0.96f),
			BattleTileRarity.Epic => new Color(0.18f, 0.08f, 0.29f, 0.96f),
			BattleTileRarity.Rare => new Color(0.045f, 0.15f, 0.24f, 0.96f),
			_ => new Color(0.08f, 0.1f, 0.09f, 0.96f)
		};
	}

	private static Color ResolveForgeSlotOutlineColor(BattleTileRarity rarity)
	{
		return rarity switch
		{
			BattleTileRarity.Mythic => new Color(1f, 0.3f, 0.38f, 0.9f),
			BattleTileRarity.Legendary => new Color(1f, 0.72f, 0.22f, 0.9f),
			BattleTileRarity.Epic => new Color(0.72f, 0.43f, 1f, 0.9f),
			BattleTileRarity.Rare => new Color(0.38f, 0.74f, 1f, 0.9f),
			_ => new Color(0.8f, 0.82f, 0.76f, 0.84f)
		};
	}

	private static string ResolveTileName(BattleTileData data, string tileId)
	{
		string text = ((!string.IsNullOrWhiteSpace(data?.DisplayName)) ? data.DisplayName.Trim() : (string.IsNullOrWhiteSpace(tileId) ? "Stone" : tileId.Trim()));
		return NormalizeBattleTileId(tileId) switch
		{
			"29" => T("Рубиновый гребень феникса", text, "Yakut Anka Arması", "Rubin-Phonixwappen"), 
			"30" => T("Сапфировая ледяная корона", text, "Safir Buz Tacı", "Saphir-Frostkrone"), 
			"31" => T("Изумрудный щит змея", text, "Zümrüt Yılan Kalkanı", "Smaragd-Schlangenschild"), 
			"32" => T("Аметистовое око пустоты", text, "Ametist Boşluk Gözü", "Amethyst-Auge der Leere"), 
			"33" => T("Золотая маска дракона", text, "Altın Ejder Maskesi", "Goldene Drachenmaske"), 
			"34" => T("Багровая маска демона", text, "Kızıl İblis Maskesi", "Purpurne Daemonenmaske"), 
			"35" => T("Циановый кристальный меч", text, "Camgöbeği Kristal Kılıç", "Cyan-Kristallschwert"), 
			"37" => T("Золотое солнце льва", text, "Altın Aslan Güneşi", "Goldene Loewensonne"), 
			"38" => T("Бирюзовый якорь кракена", text, "Turkuaz Kraken Çapası", "Tuerkiser Krakenanker"), 
			"39" => T("Белое святое крылатое копье", text, "Beyaz Kutsal Kanat Mızrağı", "Weisser heiliger Fluegelspeer"), 
			"40" => T("Зеленый фонарь некроманта", text, "Yeşil Nekromant Feneri", "Gruene Nekromantenlaterne"), 
			"41" => T("Синий громовой молот", text, "Mavi Gökgürültüsü Çekici", "Blauer Donnerhammer"), 
			"42" => T("Реликвия розового кинжала", text, "Gül Hançer Yadigarı", "Rosendolch-Relikt"), 
			"43" => T("Щит грифона", text, "Grifon Kalkanı", "Gryphonschild"), 
			"44" => T("Фиолетовый ключ портала", text, "Mor Portal Anahtarı", "Violetter Portalschluessel"), 
			"45" => T("Нефритовый лотосовый череп", text, "Yeşim Lotus Kafatası", "Jade-Lotusschaedel"), 
			"46" => T("Призматическая кристальная реликвия", text, "Prizmatik Kristal Yadigar", "Prismatisches Kristallrelikt"), 
			"47" => T("Небесные рога оленя", text, "Göksel Geyik Boynuzları", "Himmlisches Hirschgeweih"), 
			"48" => T("Лавовая рукавица титана", text, "Magma Titan Eldiveni", "Magma-Titanenhandschuh"), 
			"49" => T("Коготь штормового орла", text, "Fırtına Kartalı Pençesi", "Sturm-Adlerkralle"), 
			"50" => T("Теневая коса песочных часов", text, "Gölge Orak Kumsaati", "Schatten-Sensenstundenglas"), 
			"51" => T("Коралловый трезубец раковины", text, "Mercan Deniz Kabuğu Üçlü Mızrağı", "Korallenmuschel-Dreizack"), 
			"52" => T("Нефритовая маска самурая", text, "Yeşim Samuray Maskesi", "Jade-Samuraimaske"), 
			"53" => T("Бронзовый топор минотавра", text, "Bronz Minotor Baltası", "Bronzene Minotaurenaxt"), 
			"54" => T("Серебряный паучий кинжал", text, "Gümüş Örümcek Hançeri", "Silberner Spinnendolch"), 
			"55" => T("Лотосовый лунный посох", text, "Lotus Ay Asası", "Lotus-Mondstab"), 
			"56" => T("Лук кровавой луны", text, "Kanlı Ay Yayı", "Blutmondbogen"), 
			"57" => T("Маска чумного ворона", text, "Veba Kuzgun Maskesi", "Pestdoktor-Rabenmaske"), 
			"58" => T("Солнечный жреческий анкх", text, "Güneş Rahibi Ankh", "Sonnenpriester-Ankh"), 
			"59" => T("Топор ледяного волка", text, "Buz Kurt Baltası", "Frostwolf-Axt"), 
			"60" => T("Ониксовый гробовой щит", text, "Oniks Tabut Kalkanı", "Onyx-Sargschild"), 
			"61" => T("Изумрудный рог друида", text, "Zümrüt Druid Boynuzu", "Smaragd-Druidenhorn"), 
			"62" => T("Наконечник кометного копья", text, "Kuyruklu Yıldız Mızrak Ucu", "Kometen-Speerspitze"), 
			"63" => T("Королевское копье шахматного коня", text, "Kraliyet Satranç Atı Mızrağı", "Koenigliche Schachritterlanze"), 
			"64" => T("Багровая чаша вампира", text, "Kızıl Vampir Kadehi", "Purpurner Vampirkelch"), 
			_ => text, 
		};
	}

	private static string ResolveTileDescription(BattleTileData data, string tileId)
	{
		string text = ResolveTileName(data, tileId);
		if (!string.IsNullOrWhiteSpace(data?.Description))
		{
			return T("Особый battle-камень «" + text + "». Его пассивные свойства усиливают героя, а активный эффект раскрывается при совпадении пары.", data.Description.Trim(), text + " özel bir savaş taşıdır. Pasif özellikleri kahramanı güçlendirir; aktif etkisi eşleşme sırasında açılır.", text + " ist ein besonderer Battle-Stein. Passive Werte staerken den Helden, aktive Werte wirken beim Paar-Treffer.");
		}
		return T("Боевой камень: его пассивные свойства усиливают героя, а активные свойства раскрываются во время совпадений.", "Battle stone: passive traits empower the hero, while active traits trigger during matches.", "Savaş taşı: pasif özellikleri kahramanı güçlendirir, aktif özellikleri eşleşmelerde açılır.", "Kampfstein: passive Werte staerken den Helden, aktive Werte wirken bei Paaren.");
	}

	private static void GetUpgradePowerMultipliers(BattleTileData data, int newLevel, out float previousPower, out float nextPower)
	{
		BattleTileRarity rarity = data?.Rarity ?? BattleTileRarity.Rare;
		int previousLevel = Mathf.Max(0, newLevel - 1);
		float rarityPower = BattleTileInventoryService.GetRarityPowerMultiplier(rarity);
		previousPower = rarityPower * (1f + (float)previousLevel * 0.1f);
		nextPower = rarityPower * (1f + (float)Mathf.Max(0, newLevel) * 0.1f);
	}

	private static string NormalizeBattleTileId(string tileId)
	{
		if (string.IsNullOrWhiteSpace(tileId))
		{
			return string.Empty;
		}
		string text = tileId.Trim();
		if (!text.StartsWith("battle_tile_", StringComparison.OrdinalIgnoreCase))
		{
			return text.TrimStart('0');
		}
		return text.Substring("battle_tile_".Length).TrimStart('0');
	}

	private static string BuildBonusUpgradeSummary(BattleTileBonusData bonus, float previousPower, float nextPower)
	{
		if (bonus == null || !bonus.HasAnyBonus())
		{
			return string.Empty;
		}
		string text = string.Empty;
		AppendIntUpgrade(ref text, "HP", bonus.MaxHp, previousPower, nextPower);
		AppendIntUpgrade(ref text, T("Атака", "Attack", "Saldırı", "Angriff"), bonus.Attack, previousPower, nextPower);
		AppendPercentUpgrade(ref text, T("Броня", "Armor", "Zırh", "Rüstung"), bonus.Armor, previousPower, nextPower);
		AppendPercentUpgrade(ref text, T("Парирование", "Parry", "Savunma", "Parade"), bonus.ParryChance, previousPower, nextPower);
		AppendPercentUpgrade(ref text, T("Шанс крит. удара", "Critical Chance", "Kritik Şansı", "Kritische Chance"), bonus.CritChance, previousPower, nextPower);
		if (bonus.CritDamageMultiplier > 1f)
		{
			AppendPercentUpgrade(ref text, T("Критический урон", "Critical Damage", "Kritik Hasar", "Kritischer Schaden"), bonus.CritDamageMultiplier - 1f, previousPower, nextPower);
		}
		return text;
	}

	private static string BuildActiveBonusUpgradeSummary(BattleTileActiveBonusData bonus, float previousPower, float nextPower)
	{
		if (bonus == null || !bonus.HasAnyBonus())
		{
			return string.Empty;
		}
		string text = string.Empty;
		AppendIntUpgrade(ref text, T("Удар", "Hit", "Vuruş", "Treffer"), bonus.Attack, previousPower, nextPower);
		AppendIntUpgrade(ref text, T("Лечение", "Heal", "İyileşme", "Heilung"), bonus.HealSelf, previousPower, nextPower);
		AppendPercentUpgrade(ref text, T("Шанс крит. удара", "Critical Chance", "Kritik Şansı", "Kritische Chance"), bonus.CritChance, previousPower, nextPower);
		if (bonus.CritDamageMultiplier > 1f)
		{
			AppendPercentUpgrade(ref text, T("Критический урон", "Critical Damage", "Kritik Hasar", "Kritischer Schaden"), bonus.CritDamageMultiplier - 1f, previousPower, nextPower);
		}
		return text;
	}

	private static void AppendIntUpgrade(ref string text, string label, int amount, float previousPower, float nextPower)
	{
		if (amount > 0)
		{
			int previousValue = Mathf.RoundToInt((float)amount * previousPower);
			int nextValue = Mathf.RoundToInt((float)amount * nextPower);
			AppendUpgradePart(ref text, $"{label}: +{nextValue}  <color=#74E8A5>(+{Mathf.Max(0, nextValue - previousValue)})</color>");
		}
	}

	private static void AppendPercentUpgrade(ref string text, string label, float amount, float previousPower, float nextPower)
	{
		if (!(amount <= 0f))
		{
			int previousValue = Mathf.RoundToInt(amount * previousPower * 100f);
			int nextValue = Mathf.RoundToInt(amount * nextPower * 100f);
			AppendUpgradePart(ref text, $"{label}: +{nextValue}%  <color=#74E8A5>(+{Mathf.Max(0, nextValue - previousValue)}%)</color>");
		}
	}

	private static void AppendUpgradePart(ref string text, string value)
	{
		if (!string.IsNullOrEmpty(text))
		{
			text += "\n";
		}
		text += value;
	}

	private static string BuildBonusSummary(BattleTileBonusData bonus)
	{
		if (bonus == null || !bonus.HasAnyBonus())
		{
			return string.Empty;
		}
		string text = string.Empty;
		AppendBonus(ref text, "HP", bonus.MaxHp);
		AppendBonus(ref text, T("Атака", "Attack", "Saldırı", "Angriff"), bonus.Attack);
		AppendPercentBonus(ref text, "Armor", bonus.Armor);
		AppendPercentBonus(ref text, "Crit", bonus.CritChance);
		if (bonus.CritDamageMultiplier > 1f)
		{
			AppendPercentBonus(ref text, "Crit DMG", bonus.CritDamageMultiplier - 1f);
		}
		return text;
	}

	private static string BuildActiveBonusSummary(BattleTileActiveBonusData bonus)
	{
		if (bonus == null || !bonus.HasAnyBonus())
		{
			return string.Empty;
		}
		string text = string.Empty;
		AppendBonus(ref text, T("Удар", "Hit", "Vuruş", "Treffer"), bonus.Attack);
		AppendBonus(ref text, T("Лечение", "Heal", "İyileşme", "Heilung"), bonus.HealSelf);
		AppendPercentBonus(ref text, "Crit", bonus.CritChance);
		if (bonus.CritDamageMultiplier > 1f)
		{
			AppendPercentBonus(ref text, "Crit DMG", bonus.CritDamageMultiplier - 1f);
		}
		return text;
	}

	private static void AppendBonus(ref string text, string label, int amount)
	{
		if (amount > 0)
		{
			if (!string.IsNullOrEmpty(text))
			{
				text += ", ";
			}
			text += $"+{amount} {label}";
		}
	}

	private static void AppendPercentBonus(ref string text, string label, float amount)
	{
		int num = Mathf.RoundToInt(amount * 100f);
		if (num > 0)
		{
			if (!string.IsNullOrEmpty(text))
			{
				text += ", ";
			}
			text += $"+{num}% {label}";
		}
	}

	private PlayerProfile GetProfile()
	{
		EnsureProfileServices();
		if (!(ProfileService.I != null))
		{
			return null;
		}
		return ProfileService.I.Current;
	}

	private static void EnsureProfileServices()
	{
		if (ProfileService.I == null || CurrencyService.I == null)
		{
			ProfileRuntimeBootstrap.EnsureServices();
		}
		if (ProfileService.I != null && ProfileService.I.Current == null)
		{
			ProfileRuntimeBootstrap.TryLoadCachedProfile();
		}
	}

	private static Canvas GetOrCreateRuntimeCanvas(Scene scene)
	{
		if (!scene.IsValid())
		{
			scene = SceneManager.GetActiveScene();
		}
		Canvas canvas = FindRuntimeCanvas(scene);
		if (canvas != null)
		{
			ConfigureRuntimeCanvas(canvas);
			return canvas;
		}
		GameObject obj = new GameObject("BattleStoneForgeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		SceneManager.MoveGameObjectToScene(obj, scene);
		RectTransform component = obj.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		Canvas component2 = obj.GetComponent<Canvas>();
		ConfigureRuntimeCanvas(component2);
		return component2;
	}

	private static Sprite LoadOzTileIcon()
	{
		if (cachedOzTileIcon != null)
		{
			return cachedOzTileIcon;
		}
		cachedOzTileIcon = Resources.Load<Sprite>("Mahjong/Sprites/BattleTiles/OzTile");
		if (cachedOzTileIcon != null)
		{
			return cachedOzTileIcon;
		}
		Sprite[] array = Resources.LoadAll<Sprite>("Mahjong/Sprites/BattleTiles/OzTile");
		if (array != null && array.Length != 0)
		{
			cachedOzTileIcon = array[0];
		}
		return cachedOzTileIcon;
	}

	private static Canvas FindRuntimeCanvas(Scene scene)
	{
		Canvas[] array = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
		foreach (Canvas canvas in array)
		{
			if (canvas != null && canvas.gameObject.scene == scene && string.Equals(canvas.gameObject.name, "BattleStoneForgeCanvas", StringComparison.Ordinal))
			{
				return canvas;
			}
		}
		return null;
	}

	private static void ConfigureRuntimeCanvas(Canvas canvas)
	{
		if (!(canvas == null))
		{
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.overrideSorting = true;
			canvas.sortingOrder = 30042;
			CanvasScaler canvasScaler = canvas.GetComponent<CanvasScaler>();
			if (canvasScaler == null)
			{
				canvasScaler = canvas.gameObject.AddComponent<CanvasScaler>();
			}
			MainLobbyUiCoordinator.ConfigureOverlayScaler(canvasScaler);
			if (canvas.GetComponent<GraphicRaycaster>() == null)
			{
				canvas.gameObject.AddComponent<GraphicRaycaster>();
			}
		}
	}

	private Transform CreateScrollContent(Transform parent, string objectName, Vector2 position, Vector2 size)
	{
		GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = size;
		Image component2 = gameObject.GetComponent<Image>();
		component2.color = Color.clear;
		component2.raycastTarget = true;
		GameObject gameObject2 = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
		gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
		RectTransform component3 = gameObject2.GetComponent<RectTransform>();
		component3.anchorMin = Vector2.zero;
		component3.anchorMax = Vector2.one;
		component3.offsetMin = Vector2.zero;
		component3.offsetMax = Vector2.zero;
		GameObject obj = new GameObject("Content", typeof(RectTransform));
		obj.transform.SetParent(gameObject2.transform, worldPositionStays: false);
		RectTransform component4 = obj.GetComponent<RectTransform>();
		component4.anchorMin = new Vector2(0.5f, 1f);
		component4.anchorMax = new Vector2(0.5f, 1f);
		component4.pivot = new Vector2(0.5f, 1f);
		component4.anchoredPosition = Vector2.zero;
		component4.sizeDelta = size;
		ScrollRect component5 = gameObject.GetComponent<ScrollRect>();
		component5.viewport = component3;
		component5.content = component4;
		component5.horizontal = false;
		component5.vertical = true;
		component5.movementType = ScrollRect.MovementType.Clamped;
		component5.scrollSensitivity = 36f;
		component5.inertia = true;
		return obj.transform;
	}

	private Button CreateStoneRow(Transform parent, string objectName, BattleTileData data, string tileId, int count, int level, bool selected, Vector2 position, UnityAction action)
	{
		GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 1f);
		component.anchorMax = new Vector2(0.5f, 1f);
		component.pivot = new Vector2(0.5f, 1f);
		component.anchoredPosition = position;
		component.sizeDelta = new Vector2(126f, 166f);
		Image component2 = gameObject.GetComponent<Image>();
		component2.color = Color.clear;
		component2.raycastTarget = true;
		Button component3 = gameObject.GetComponent<Button>();
		if (action != null)
		{
			component3.onClick.AddListener(action);
		}
		Image image = CreateImage(gameObject.transform, "Face", new Vector2(0f, 7f), new Vector2(104f, 150f));
		image.sprite = ((data?.Prefab != null) ? data.Prefab.FaceSprite : null);
		image.enabled = image.sprite != null;
		image.raycastTarget = false;
		Outline faceOutline = image.gameObject.AddComponent<Outline>();
		faceOutline.effectColor = selected ? new Color(1f, 0.62f, 0.18f, 0.95f) : new Color(0.48f, 0.3f, 0.1f, 0.8f);
		faceOutline.effectDistance = selected ? new Vector2(3f, -3f) : new Vector2(1.5f, -1.5f);
		Shadow faceShadow = image.gameObject.AddComponent<Shadow>();
		faceShadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
		faceShadow.effectDistance = new Vector2(5f, -7f);
		component3.targetGraphic = image;
		ColorBlock colors = component3.colors;
		colors.normalColor = Color.white;
		colors.highlightedColor = new Color(1.08f, 1.03f, 0.88f, 1f);
		colors.pressedColor = new Color(0.82f, 0.7f, 0.5f, 1f);
		colors.selectedColor = colors.highlightedColor;
		colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);
		colors.colorMultiplier = 1f;
		component3.colors = colors;
		if (!image.enabled)
		{
			CreateText(gameObject.transform, "FaceFallback", "?", new Vector2(0f, 18f), new Vector2(54f, 54f), 32f, TextAlignmentOptions.Center);
		}
		if (count > 1)
		{
			Image quantityBadge = CreateImage(gameObject.transform, "Count", new Vector2(42f, -56f), new Vector2(48f, 38f));
			quantityBadge.preserveAspect = false;
			quantityBadge.sprite = BattlePopupStyle.SmallButtonSprite;
			quantityBadge.type = quantityBadge.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
			quantityBadge.color = new Color(0.22f, 0.12f, 0.035f, 0.98f);
			Outline badgeOutline = quantityBadge.gameObject.AddComponent<Outline>();
			badgeOutline.effectColor = new Color(1f, 0.66f, 0.18f, 0.95f);
			badgeOutline.effectDistance = new Vector2(1.5f, -1.5f);
			TMP_Text quantityText = CreateText(quantityBadge.transform, "Value", Mathf.Max(0, count).ToString(), Vector2.zero, new Vector2(42f, 30f), 23f, TextAlignmentOptions.Center);
			quantityText.color = new Color(1f, 0.88f, 0.55f, 1f);
			quantityText.fontStyle = FontStyles.Bold;
			quantityText.raycastTarget = false;
		}
		CreateForgeUpgradeStars(gameObject.transform, level, new Vector2(0f, 7f), new Vector2(104f, 150f));
		return component3;
	}

	private void CreateForgeUpgradeStars(Transform parent, int level, Vector2 facePosition, Vector2 faceSize)
	{
		BattleTileUpgradeVisual.Apply(parent, facePosition, faceSize, level);
	}

	private Button CreateButton(Transform parent, string objectName, string label, Vector2 position, Vector2 size, UnityAction action)
	{
		GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = size;
		Button component2 = gameObject.GetComponent<Button>();
		component2.targetGraphic = gameObject.GetComponent<Image>();
		if (action != null)
		{
			component2.onClick.AddListener(action);
		}
		BattlePopupStyle.ApplyButton(component2);
		CreateText(gameObject.transform, "Label", label, Vector2.zero, size, Mathf.Clamp(size.y * 0.42f, 20f, 34f), TextAlignmentOptions.Center).raycastTarget = false;
		return component2;
	}

	private GameObject CreatePanel(Transform parent, string objectName, Vector2 size, Vector2 position)
	{
		GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		obj.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = obj.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = size;
		Image component2 = obj.GetComponent<Image>();
		if (!BattlePopupStyle.ApplyWindow(component2))
		{
			component2.color = new Color(0.08f, 0.09f, 0.14f, 0.96f);
			component2.raycastTarget = true;
		}
		return obj;
	}

	private Image CreateImage(Transform parent, string objectName, Vector2 position, Vector2 size)
	{
		GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		obj.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = obj.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = size;
		Image image = obj.GetComponent<Image>();
		image.preserveAspect = true;
		image.raycastTarget = false;
		return image;
	}

	private void PlayForgeHammerSequence()
	{
		if (!IsSoundEnabled())
		{
			return;
		}
		AudioClip audioClip = ResolveForgeHammerClip();
		if (!(audioClip == null))
		{
			EnsureForgeAudioSource();
			if (!(forgeAudioSource == null))
			{
				forgeAudioSource.PlayOneShot(audioClip, 0.82f);
			}
		}
	}

	private void PlayVictorySound()
	{
		if (!IsSoundEnabled())
		{
			return;
		}
		AudioClip audioClip = ResolveVictoryClip();
		if (!(audioClip == null))
		{
			EnsureForgeAudioSource();
			if (!(forgeAudioSource == null))
			{
				forgeAudioSource.PlayOneShot(audioClip, 0.88f);
			}
		}
	}

	private void EnsureForgeAudioSource()
	{
		if (!(forgeAudioSource != null))
		{
			forgeAudioSource = GetComponent<AudioSource>();
			if (forgeAudioSource == null)
			{
				forgeAudioSource = base.gameObject.AddComponent<AudioSource>();
			}
			forgeAudioSource.playOnAwake = false;
			forgeAudioSource.loop = false;
			forgeAudioSource.spatialBlend = 0f;
		}
	}

	private static AudioClip ResolveForgeHammerClip()
	{
		if (cachedForgeHammerClip == null)
		{
			cachedForgeHammerClip = Resources.Load<AudioClip>("Mahjong/Sounds/ForgeHammerHit");
		}
		return cachedForgeHammerClip;
	}

	private static Sprite ResolveForgeGlowSprite()
	{
		if (cachedForgeGlowSprite != null)
		{
			return cachedForgeGlowSprite;
		}
		Texture2D texture2D = new Texture2D(96, 96, TextureFormat.RGBA32, mipChain: false)
		{
			name = "ForgeRadialGlowTexture",
			filterMode = FilterMode.Bilinear,
			wrapMode = TextureWrapMode.Clamp
		};
		Color[] array = new Color[9216];
		Vector2 b = new Vector2(47.5f, 47.5f);
		float num = 48f;
		for (int i = 0; i < 96; i++)
		{
			for (int j = 0; j < 96; j++)
			{
				float num2 = Vector2.Distance(new Vector2(j, i), b) / num;
				float t = Mathf.Clamp01(1f - num2);
				float num3 = Mathf.SmoothStep(0f, 1f, t);
				num3 *= num3;
				array[i * 96 + j] = new Color(1f, 1f, 1f, num3);
			}
		}
		texture2D.SetPixels(array);
		texture2D.Apply(updateMipmaps: false, makeNoLongerReadable: true);
		cachedForgeGlowSprite = Sprite.Create(texture2D, new Rect(0f, 0f, 96f, 96f), new Vector2(0.5f, 0.5f), 96f);
		cachedForgeGlowSprite.name = "ForgeRadialGlow";
		return cachedForgeGlowSprite;
	}

	private static AudioClip ResolveVictoryClip()
	{
		if (cachedVictoryClip == null)
		{
			cachedVictoryClip = Resources.Load<AudioClip>("Mahjong/Sounds/game-won");
		}
		return cachedVictoryClip;
	}

	private static bool IsSoundEnabled()
	{
		if (!(AppSettings.I == null))
		{
			return AppSettings.I.SoundEnabled;
		}
		return true;
	}

	private TMP_Text CreateText(Transform parent, string objectName, string value, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
	{
		GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		obj.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = obj.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = size;
		TextMeshProUGUI component2 = obj.GetComponent<TextMeshProUGUI>();
		component2.text = value;
		component2.fontSize = fontSize;
		component2.enableAutoSizing = true;
		component2.fontSizeMin = Mathf.Max(12f, fontSize * 0.55f);
		component2.fontSizeMax = fontSize;
		component2.alignment = alignment;
		component2.color = Color.white;
		component2.textWrappingMode = TextWrappingModes.Normal;
		component2.raycastTarget = false;
		BattlePopupStyle.ApplyText(component2);
		return component2;
	}

	private static void ApplyForgeResultTextGlow(TMP_Text text, Color color, float outlineWidth)
	{
		if (!(text == null))
		{
			text.color = color;
			text.outlineColor = new Color(0.95f, 0.3f, 0.02f, 0.96f);
			text.outlineWidth = Mathf.Clamp(outlineWidth, 0f, 0.35f);
			text.fontStyle |= FontStyles.Bold;
		}
	}

	private static void ClearDynamicChildren(Transform parent, params string[] keepNames)
	{
		if (parent == null)
		{
			return;
		}
		for (int num = parent.childCount - 1; num >= 0; num--)
		{
			Transform child = parent.GetChild(num);
			bool flag = false;
			for (int i = 0; i < keepNames.Length; i++)
			{
				if (string.Equals(child.name, keepNames[i], StringComparison.Ordinal))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				UnityEngine.Object.Destroy(child.gameObject);
			}
		}
	}

	private static void DestroyRuntimeObject(GameObject obj)
	{
		if (obj != null)
		{
			UnityEngine.Object.Destroy(obj);
		}
	}

	private static string T(string russian, string english, string turkish, string german = null)
	{
		return ((AppSettings.I != null) ? AppSettings.I.Language : GameLanguage.Russian) switch
		{
			GameLanguage.English => english, 
			GameLanguage.Turkish => turkish, 
			GameLanguage.German => german ?? english, 
			_ => russian, 
		};
	}
}
}
