using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{

[DisallowMultipleComponent]
public sealed class BattleStoneAuctionUI : MonoBehaviour
{
	private enum AuctionTab
	{
		Market,
		Sell,
		MyLots
	}

	private enum AuctionSaleMode
	{
		FixedPrice,
		LiveBid
	}

	private enum AuctionLotStatus
	{
		Active,
		Sold,
		Expired,
		Cancelled
	}

	private enum AuctionRarityFilter
	{
		All,
		Rare,
		Epic,
		Legendary,
		Mythic
	}

	private sealed class AuctionLot
	{
		public string LotId;

		public string SellerId;

		public string SellerName;

		public string TileId;

		public AuctionSaleMode SaleMode;

		public AuctionLotStatus Status;

		public int FixedPrice;

		public int StartPrice;

		public int CurrentBid;

		public int MinBidStep;

		public string HighestBidderId;

		public string HighestBidderName;

		public float EndsAtRealtime;
	}

	private sealed class AuctionSaleDropSlot : MonoBehaviour, IDropHandler, IEventSystemHandler
	{
		private BattleStoneAuctionUI owner;

		public void Configure(BattleStoneAuctionUI owner)
		{
			this.owner = owner;
		}

		public void OnDrop(PointerEventData eventData)
		{
			AuctionStoneDragSource auctionStoneDragSource = eventData?.pointerDrag?.GetComponent<AuctionStoneDragSource>();
			if (auctionStoneDragSource != null)
			{
				owner?.SelectTileForSale(auctionStoneDragSource.TileId);
			}
		}
	}

	private sealed class AuctionStoneDragSource : MonoBehaviour, IPointerClickHandler, IEventSystemHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		private BattleStoneAuctionUI owner;

		private string tileId;

		private GameObject dragGhost;

		private RectTransform dragGhostRect;

		public string TileId => tileId;

		public void Configure(BattleStoneAuctionUI owner, string tileId)
		{
			this.owner = owner;
			this.tileId = tileId;
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			owner?.SelectTileForSale(tileId);
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (!(owner == null) && !string.IsNullOrWhiteSpace(tileId))
			{
				Canvas rootCanvas = owner.rootCanvas;
				if (!(rootCanvas == null))
				{
					dragGhost = new GameObject("AuctionStoneDragGhost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
					dragGhost.transform.SetParent(rootCanvas.transform, worldPositionStays: false);
					dragGhost.transform.SetAsLastSibling();
					dragGhostRect = dragGhost.GetComponent<RectTransform>();
					dragGhostRect.sizeDelta = new Vector2(150f, 178f);
					Image component = dragGhost.GetComponent<Image>();
					component.sprite = ResolveTileSprite(ResolveTileData(tileId));
					component.color = new Color(1f, 1f, 1f, 0.86f);
					component.preserveAspect = true;
					component.raycastTarget = false;
					MoveGhost(eventData);
				}
			}
		}

		public void OnDrag(PointerEventData eventData)
		{
			MoveGhost(eventData);
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			owner?.TryDropTileToSaleSlot(tileId, eventData);
			if (dragGhost != null)
			{
				UnityEngine.Object.Destroy(dragGhost);
			}
			dragGhost = null;
			dragGhostRect = null;
		}

		private void MoveGhost(PointerEventData eventData)
		{
			if (!(dragGhostRect == null) && eventData != null)
			{
				RectTransform rectTransform = ((owner != null && owner.rootCanvas != null) ? (owner.rootCanvas.transform as RectTransform) : null);
				if (!(rectTransform == null))
				{
					RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out var localPoint);
					dragGhostRect.anchoredPosition = localPoint;
				}
			}
		}
	}

	private const string LobbySceneName = "LobbyMahjongBattle";

	private const string HostObjectName = "BattleStoneAuctionUI";

	private const string RuntimeCanvasName = "BattleStoneAuctionCanvas";

	private const string OpenButtonName = "ButtonBattleStoneAuction";

	private const string OverlayName = "BattleStoneAuctionOverlay";

	private const int RuntimeCanvasSortingOrder = 30040;

	private const int CommonAuctionFloorPrice = 200;

	private const int RareAuctionFloorPrice = 800;

	private const int EpicAuctionFloorPrice = 2500;

	private const int LegendaryAuctionFloorPrice = 12000;

	private const int MythicAuctionFloorPrice = 45000;

	private static readonly Vector2 OpenButtonSize = new Vector2(340f, 92f);

	private static readonly List<AuctionLot> lots = new List<AuctionLot>();

	private static int nextLotNumber = 1;

	private Canvas rootCanvas;

	private Button openButton;

	private GameObject overlayRoot;

	private Transform listRoot;

	private TMP_Text titleText;

	private TMP_Text balanceText;

	private TMP_Text statusText;

	private TMP_InputField priceInput;

	private TMP_InputField durationInput;

	private Button fixedModeButton;

	private Button bidModeButton;

	private RectTransform saleSlotRect;

	private AuctionTab activeTab;

	private AuctionSaleMode selectedSaleMode;

	private AuctionRarityFilter sellRarityFilter;

	private string selectedTileId = string.Empty;

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
		BattleStoneAuctionUI[] array = UnityEngine.Object.FindObjectsByType<BattleStoneAuctionUI>(FindObjectsInactive.Include);
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
			GameObject obj = new GameObject("BattleStoneAuctionUI", typeof(RectTransform), typeof(BattleStoneAuctionUI));
			SceneManager.MoveGameObjectToScene(obj, scene);
			obj.transform.SetParent(orCreateRuntimeCanvas.transform, worldPositionStays: false);
		}
	}

	private void Awake()
	{
		EnsureProfileServices();
		rootCanvas = GetComponentInParent<Canvas>();
		EnsureOpenButton();
	}

	private void OnEnable()
	{
		CurrencyService.CurrencyChanged += RefreshIfVisible;
		ProfileService.ProfileChanged += RefreshIfVisible;
		EnsureOpenButton();
	}

	private void OnDisable()
	{
		CurrencyService.CurrencyChanged -= RefreshIfVisible;
		ProfileService.ProfileChanged -= RefreshIfVisible;
	}

	private void Update()
	{
		if (ResolveExpiredLots() && overlayRoot != null && overlayRoot.activeSelf)
		{
			RefreshWindow();
		}
	}

	private void OnDestroy()
	{
		SetForgeUiVisible(visible: true);
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
				GameObject gameObject = new GameObject("ButtonBattleStoneAuction", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
				gameObject.transform.SetParent(buttonCanvas.transform, worldPositionStays: false);
				MainLobbyUiCoordinator.LayoutBattleLobbyTopTabButton(gameObject.GetComponent<Button>(), 1, 4, GetCanvasSize(buttonCanvas));
				Image component = gameObject.GetComponent<Image>();
				component.color = Color.white;
				openButton = gameObject.GetComponent<Button>();
				openButton.targetGraphic = component;
				openButton.onClick.AddListener(OpenWindow);
				BattlePopupStyle.ApplyButton(openButton);
				CreateText(gameObject.transform, "Label", T("Аукцион", "Auction", "Açık Artırma", "Auktion"), Vector2.zero, OpenButtonSize, 34f, TextAlignmentOptions.Center).raycastTarget = false;
				BattlePopupStyle.ApplyBattleLobbyUtilityButton(openButton);
			}
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
		EnsureWindow();
		if (!(overlayRoot == null))
		{
			BattleLobbyUiCoordinator.OpenModal(BattleLobbyModalKind.Auction);
			SetForgeUiVisible(visible: false);
			overlayRoot.SetActive(value: true);
			overlayRoot.transform.SetAsLastSibling();
		}
	}

	private void CloseWindow()
	{
		if (overlayRoot != null)
		{
			overlayRoot.SetActive(value: false);
		}
		SetForgeUiVisible(visible: true);
		BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.Auction);
	}

	private void EnsureWindow()
	{
		if (overlayRoot != null)
		{
			return;
		}
		Canvas orCreateRuntimeCanvas = GetOrCreateRuntimeCanvas(base.gameObject.scene);
		if (!(orCreateRuntimeCanvas == null))
		{
			overlayRoot = new GameObject("BattleStoneAuctionOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			overlayRoot.transform.SetParent(orCreateRuntimeCanvas.transform, worldPositionStays: false);
			RectTransform component = overlayRoot.GetComponent<RectTransform>();
			component.anchorMin = Vector2.zero;
			component.anchorMax = Vector2.one;
			component.offsetMin = Vector2.zero;
			component.offsetMax = Vector2.zero;
			Image component2 = overlayRoot.GetComponent<Image>();
			component2.color = new Color(0f, 0f, 0f, 0.68f);
			component2.raycastTarget = true;
			GameObject gameObject = CreatePanel(overlayRoot.transform, "AuctionComingSoonPanel", new Vector2(1460f, 620f), Vector2.zero);
			CreateText(gameObject.transform, "Title", T("Аукцион", "Auction", "Açık Artırma", "Auktion"), new Vector2(0f, 174f), new Vector2(980f, 72f), 62f, TextAlignmentOptions.Center);
			CreateText(gameObject.transform, "Body", T("Аукцион сейчас в разработке.", "The auction is currently in development.", "Açık artırma şu anda geliştirme aşamasında.", "Die Auktion ist derzeit in Entwicklung."), new Vector2(0f, 28f), new Vector2(1040f, 150f), 42f, TextAlignmentOptions.Center);
			CreateButton(gameObject.transform, "ButtonAuctionComingSoonOk", T("Понял", "Got it", "Anladım", "Verstanden"), new Vector2(0f, -178f), new Vector2(460f, 96f), CloseWindow);
			overlayRoot.SetActive(value: false);
		}
	}

	private void SetTab(AuctionTab tab)
	{
		activeTab = tab;
		selectedTileId = string.Empty;
		RefreshWindow();
	}

	private void RefreshIfVisible()
	{
		if (overlayRoot != null && overlayRoot.activeSelf && listRoot != null)
		{
			RefreshWindow();
		}
	}

	private void RefreshWindow()
	{
		EnsureProfileServices();
		ResolveExpiredLots();
		ClearChildren(listRoot);
		titleText.text = T("Аукцион камней", "Stone Auction", "Taş Açık Artırması", "Steinauktion");
		balanceText.text = T("Баланс", "Balance", "Bakiye", "Guthaben") + $": {GetGold()} OzAltın";
		switch (activeTab)
		{
		case AuctionTab.Sell:
			RebuildSellTab();
			break;
		case AuctionTab.MyLots:
			RebuildMyLotsTab();
			break;
		default:
			RebuildMarketTab();
			break;
		}
	}

	private void RebuildMarketTab()
	{
		statusText.text = T("Покупка своих лотов запрещена. Это окно уже готово для серверных PvP-лотов.", "Buying your own lots is blocked. This screen is ready for server PvP lots.", "Kendi lotunu satin alamazsin.", "Eigene Lose koennen nicht gekauft werden.");
		string localPlayerId = GetLocalPlayerId();
		int num = 0;
		for (int i = 0; i < lots.Count; i++)
		{
			AuctionLot auctionLot = lots[i];
			if (auctionLot != null && auctionLot.Status == AuctionLotStatus.Active)
			{
				CreateLotRow(auctionLot, num++, localPlayerId, marketActions: true);
			}
		}
		if (num == 0)
		{
			CreateEmptyText(T("Активных лотов пока нет.", "No active lots yet.", "Aktif lot yok.", "Noch keine aktiven Lose."));
		}
	}

	private void RebuildMyLotsTab()
	{
		statusText.text = T("Камень заморожен, пока лот активен. Live-лот можно отменить только до первой ставки.", "The stone is frozen while the lot is active. Live lots can be cancelled only before the first bid.", "Lot aktifken taş kilitlidir.", "Der Stein ist waehrend des aktiven Loses gesperrt.");
		string localPlayerId = GetLocalPlayerId();
		int num = 0;
		for (int i = 0; i < lots.Count; i++)
		{
			AuctionLot auctionLot = lots[i];
			if (auctionLot != null && string.Equals(auctionLot.SellerId, localPlayerId, StringComparison.Ordinal))
			{
				CreateLotRow(auctionLot, num++, localPlayerId, marketActions: false);
			}
		}
		if (num == 0)
		{
			CreateEmptyText(T("Ты еще не выставил камни.", "You have not listed any stones yet.", "Henuz taş koymadin.", "Du hast noch keine Steine eingestellt."));
		}
	}

	private void RebuildSellTab()
	{
		statusText.text = ((selectedTileId.Length > 0) ? (T("Выбран камень: ", "Selected stone: ", "Seçilen taş: ", "Ausgewaehlter Stein: ") + ResolveTileName(selectedTileId)) : T("Выбери камень, цену и режим продажи.", "Choose a stone, price, and sale mode.", "Taş, fiyat ve satis modu seç.", "Waehle Stein, Preis und Verkaufsmodus."));
		CreateText(listRoot, "SellHint", T("Камни для продажи", "Stones for sale", "Satilik taşlar", "Steine zum Verkauf"), new Vector2(-575f, 314f), new Vector2(760f, 42f), 34f, TextAlignmentOptions.Center);
		CreateRarityFilterTabs();
		RebuildSellTileGrid();
		GameObject gameObject = CreatePanel(listRoot, "SelectedSaleStonePanel", new Vector2(690f, 690f), new Vector2(592f, -18f));
		CreateText(gameObject.transform, "SelectedTitle", T("Лот продажи", "Sale Lot", "Satis Lotu", "Verkaufslos"), new Vector2(0f, 274f), new Vector2(500f, 42f), 34f, TextAlignmentOptions.Center);
		CreateSelectedStoneSlot(gameObject.transform);
		CreateSelectedStoneInfo(gameObject.transform);
		fixedModeButton = CreateButton(gameObject.transform, "FixedMode", T("Фикс цена", "Fixed Price", "Sabit fiyat", "Festpreis"), new Vector2(-164f, -78f), new Vector2(270f, 72f), delegate
		{
			selectedSaleMode = AuctionSaleMode.FixedPrice;
			RefreshWindow();
		});
		bidModeButton = CreateButton(gameObject.transform, "BidMode", T("Торги", "Live Bid", "Canli teklif", "Live-Gebot"), new Vector2(164f, -78f), new Vector2(270f, 72f), delegate
		{
			selectedSaleMode = AuctionSaleMode.LiveBid;
			RefreshWindow();
		});
		fixedModeButton.interactable = selectedSaleMode != AuctionSaleMode.FixedPrice;
		bidModeButton.interactable = selectedSaleMode != AuctionSaleMode.LiveBid;
		int suggestedAuctionPrice = GetSuggestedAuctionPrice(selectedTileId);
		CreateText(gameObject.transform, "PriceLabel", (selectedSaleMode == AuctionSaleMode.FixedPrice) ? T("Цена продажи", "Sale Price", "Satis fiyati", "Verkaufspreis") : T("Стартовая цена", "Start Price", "Başlangic", "Startpreis"), new Vector2(-152f, -184f), new Vector2(250f, 38f), 28f, TextAlignmentOptions.Left);
		priceInput = CreateInput(gameObject.transform, "AuctionPriceInput", suggestedAuctionPrice.ToString(), new Vector2(152f, -184f), new Vector2(270f, 66f));
		CreateText(gameObject.transform, "FloorHint", T("Мин.", "Min.", "Min.", "Min.") + $" {suggestedAuctionPrice}", new Vector2(152f, -232f), new Vector2(270f, 30f), 22f, TextAlignmentOptions.Center);
		CreateText(gameObject.transform, "DurationLabel", T("Таймер, сек.", "Timer, seç.", "Sure, sn.", "Timer, Sek."), new Vector2(-152f, -278f), new Vector2(250f, 38f), 28f, TextAlignmentOptions.Left);
		durationInput = CreateInput(gameObject.transform, "AuctionDurationInput", "60", new Vector2(152f, -278f), new Vector2(270f, 66f));
		durationInput.gameObject.SetActive(selectedSaleMode == AuctionSaleMode.LiveBid);
		CreateButton(gameObject.transform, "CreateLot", T("Выставить", "List Stone", "Listele", "Einstellen"), new Vector2(0f, -318f), new Vector2(500f, 74f), CreateSelectedLot);
	}

	private void CreateRarityFilterTabs()
	{
		AuctionRarityFilter[] array = new AuctionRarityFilter[5]
		{
			AuctionRarityFilter.All,
			AuctionRarityFilter.Rare,
			AuctionRarityFilter.Epic,
			AuctionRarityFilter.Legendary,
			AuctionRarityFilter.Mythic
		};
		for (int i = 0; i < array.Length; i++)
		{
			AuctionRarityFilter filter = array[i];
			Button button = CreateButton(listRoot, "RarityTab" + filter, FilterLabel(filter), new Vector2(-930f + (float)i * 178f, 260f), new Vector2(156f, 56f), delegate
			{
				sellRarityFilter = filter;
				selectedTileId = string.Empty;
				RefreshWindow();
			});
			if (sellRarityFilter == filter)
			{
				BattlePopupStyle.ApplyPremiumButton(button);
			}
		}
	}

	private void SelectTileForSale(string tileId)
	{
		selectedTileId = (string.IsNullOrWhiteSpace(tileId) ? string.Empty : tileId.Trim());
		RefreshWindow();
	}

	private void TryDropTileToSaleSlot(string tileId, PointerEventData eventData)
	{
		if (!(saleSlotRect == null) && !string.IsNullOrWhiteSpace(tileId))
		{
			Camera cam = eventData?.pressEventCamera;
			Vector2 screenPoint = eventData?.position ?? Vector2.zero;
			if (RectTransformUtility.RectangleContainsScreenPoint(saleSlotRect, screenPoint, cam))
			{
				SelectTileForSale(tileId);
			}
		}
	}

	private void RebuildSellTileGrid()
	{
		PlayerProfile profile = GetProfile();
		BattleTileStore i = BattleTileStore.I;
		if (profile == null || i == null)
		{
			CreateEmptyText(T("Профиль или хранилище камней еще загружается.", "Profile or stone store is still loading.", "Profil yukleniyor.", "Profil oder Steinspeicher laedt noch."));
			return;
		}
		BattleTileInventoryService.EnsureInventoryForStore(profile, i);
		MahjongBattleTileInventoryData orCreateInventory = BattleTileInventoryService.GetOrCreateInventory(profile);
		List<string> list = new List<string>();
		AddUnique(list, orCreateInventory.ReserveTileIds);
		AddUnique(list, orCreateInventory.ActiveTileIds);
		int num = 0;
		for (int j = 0; j < list.Count; j++)
		{
			if (num >= 15)
			{
				break;
			}
			string tileId = list[j];
			if (!IsTileListed(tileId) && IsAuctionSellable(tileId) && MatchesSellRarityFilter(tileId) && !string.Equals(tileId, selectedTileId, StringComparison.Ordinal))
			{
				int num2 = num % 5;
				int num3 = num / 5;
				CreateStoneButton(position: new Vector2(-920f + (float)num2 * 176f, 170f - (float)num3 * 186f), parent: listRoot, objectName: "SellTile_" + tileId, tileId: tileId, size: new Vector2(150f, 178f), action: delegate
				{
					SelectTileForSale(tileId);
				});
				num++;
			}
		}
		if (num == 0)
		{
			CreateText(listRoot, "NoSellTiles", T("Нет свободных камней для продажи.", "No free stones to sell.", "Satilacak bos taş yok.", "Keine freien Steine zum Verkauf."), new Vector2(-610f, 0f), new Vector2(640f, 80f), 34f, TextAlignmentOptions.Center);
		}
	}

	private void CreateSelectedLot()
	{
		if (string.IsNullOrWhiteSpace(selectedTileId))
		{
			statusText.text = T("Сначала выбери камень.", "Choose a stone first.", "Once taş seç.", "Waehle zuerst einen Stein.");
			return;
		}
		int suggestedAuctionPrice = GetSuggestedAuctionPrice(selectedTileId);
		int num = Mathf.Max(ParsePositiveInt(priceInput, suggestedAuctionPrice), suggestedAuctionPrice);
		int value = ParsePositiveInt(durationInput, 60);
		if (!TryFreezeTile(selectedTileId, out var reason))
		{
			statusText.text = reason;
			return;
		}
		AuctionLot item = new AuctionLot
		{
			LotId = "local_lot_" + nextLotNumber++,
			SellerId = GetLocalPlayerId(),
			SellerName = GetLocalPlayerName(),
			TileId = selectedTileId,
			SaleMode = selectedSaleMode,
			Status = AuctionLotStatus.Active,
			FixedPrice = ((selectedSaleMode == AuctionSaleMode.FixedPrice) ? num : 0),
			StartPrice = ((selectedSaleMode == AuctionSaleMode.LiveBid) ? num : 0),
			CurrentBid = 0,
			MinBidStep = GetMinBidStep(num),
			EndsAtRealtime = ((selectedSaleMode == AuctionSaleMode.LiveBid) ? (Time.realtimeSinceStartup + (float)Mathf.Clamp(value, 15, 600)) : 0f)
		};
		lots.Add(item);
		selectedTileId = string.Empty;
		ProfileService.I?.Save();
		ProfileService.I?.NotifyProfileChanged();
		activeTab = AuctionTab.MyLots;
		RefreshWindow();
	}

	private void CreateLotRow(AuctionLot lot, int row, string localId, bool marketActions)
	{
		GameObject gameObject = CreatePanel(position: new Vector2(0f, 286f - (float)row * 118f), parent: listRoot, objectName: "Lot_" + lot.LotId, size: new Vector2(1760f, 102f));
		BattleTileData battleTileData = ResolveTileData(lot.TileId);
		Image image = CreateImage(gameObject.transform, "StoneFace", new Vector2(-790f, 0f), new Vector2(76f, 86f));
		image.sprite = ResolveTileSprite(battleTileData);
		image.enabled = image.sprite != null;
		if (!image.enabled)
		{
			CreateText(gameObject.transform, "StoneFallback", "?", new Vector2(-790f, 0f), new Vector2(54f, 54f), 34f, TextAlignmentOptions.Center);
		}
		CreateText(gameObject.transform, "Name", ResolveTileName(lot.TileId), new Vector2(-650f, 20f), new Vector2(360f, 42f), 30f, TextAlignmentOptions.Left);
		CreateText(gameObject.transform, "Seller", ResolveRarityName(battleTileData?.Rarity ?? BattleTileRarity.Standard) + "  |  " + lot.SellerName, new Vector2(-650f, -24f), new Vector2(420f, 34f), 22f, TextAlignmentOptions.Left);
		string value = ((lot.SaleMode == AuctionSaleMode.FixedPrice) ? (T("Цена", "Price", "Fiyat", "Preis") + $": {lot.FixedPrice}") : (T("Ставка", "Bid", "Teklif", "Gebot") + $": {Mathf.Max(lot.StartPrice, lot.CurrentBid)}"));
		CreateText(gameObject.transform, "Price", value, new Vector2(-250f, 0f), new Vector2(300f, 54f), 30f, TextAlignmentOptions.Center);
		string value2 = ((lot.Status == AuctionLotStatus.Active && lot.SaleMode == AuctionSaleMode.LiveBid) ? FormatTimeLeft(lot) : LocalizeStatus(lot.Status));
		CreateText(gameObject.transform, "Timer", value2, new Vector2(120f, 0f), new Vector2(260f, 54f), 28f, TextAlignmentOptions.Center);
		if (marketActions && !string.Equals(lot.SellerId, localId, StringComparison.Ordinal))
		{
			if (lot.SaleMode == AuctionSaleMode.FixedPrice)
			{
				CreateButton(gameObject.transform, "Buy", T("Купить", "Buy", "Al", "Kaufen"), new Vector2(620f, 0f), new Vector2(260f, 76f), delegate
				{
					BuyLot(lot);
				});
			}
			else
			{
				CreateButton(gameObject.transform, "Bid", T("Ставка", "Bid", "Teklif", "Bieten"), new Vector2(620f, 0f), new Vector2(260f, 76f), delegate
				{
					PlaceBid(lot, GetNextBid(lot));
				});
			}
		}
		else if (string.Equals(lot.SellerId, localId, StringComparison.Ordinal) && lot.Status == AuctionLotStatus.Active && CanCancel(lot))
		{
			CreateButton(gameObject.transform, "Cancel", T("Отмена", "Cancel", "İptal", "Abbrechen"), new Vector2(620f, 0f), new Vector2(260f, 76f), delegate
			{
				CancelLot(lot);
			});
		}
	}

	private bool TryFreezeTile(string tileId, out string reason)
	{
		reason = string.Empty;
		PlayerProfile profile = GetProfile();
		BattleTileStore i = BattleTileStore.I;
		if (profile == null || i == null)
		{
			reason = T("Профиль еще загружается.", "Profile is still loading.", "Profil yukleniyor.", "Profil laedt noch.");
			return false;
		}
		BattleTileInventoryService.EnsureInventoryForStore(profile, i);
		MahjongBattleTileInventoryData orCreateInventory = BattleTileInventoryService.GetOrCreateInventory(profile);
		string text = tileId.Trim();
		if (!IsAuctionSellable(text))
		{
			reason = T("На аукцион можно выставлять только Rare камни и выше.", "Only Rare or higher stones can be listed.", "Sadece Rare ve üstü taşlar listelenir.", "Nur Rare-Steine oder hoeher koennen eingestellt werden.");
			return false;
		}
		if (orCreateInventory.ReserveTileIds.Remove(text))
		{
			return true;
		}
		if (string.Equals(orCreateInventory.TotemTileId, text, StringComparison.Ordinal))
		{
			reason = T("Сними камень из тотема перед продажей.", "Remove the stone from totem before selling.", "Satmadan once totemden cikar.", "Entferne den Stein vor dem Verkauf aus dem Totem.");
			return false;
		}
		if (orCreateInventory.ActiveTileIds.Count <= 2)
		{
			reason = T("Нужно оставить минимум два активных камня.", "Keep at least two active stones.", "En az iki aktif taş kalmali.", "Mindestens zwei aktive Steine behalten.");
			return false;
		}
		if (orCreateInventory.ActiveTileIds.Remove(text))
		{
			return true;
		}
		reason = T("Камень не найден в инвентаре.", "Stone was not found in inventory.", "Taş envanterde yok.", "Stein nicht im Inventar gefunden.");
		return false;
	}

	private void ReturnTileToSeller(AuctionLot lot)
	{
		MahjongBattleTileInventoryData orCreateInventory = BattleTileInventoryService.GetOrCreateInventory(GetProfile());
		if (orCreateInventory != null && !orCreateInventory.ActiveTileIds.Contains(lot.TileId) && !orCreateInventory.ReserveTileIds.Contains(lot.TileId))
		{
			orCreateInventory.ReserveTileIds.Insert(0, lot.TileId);
		}
	}

	private void BuyLot(AuctionLot lot)
	{
		if (lot != null && lot.Status == AuctionLotStatus.Active && lot.SaleMode == AuctionSaleMode.FixedPrice)
		{
			if (!CurrencyService.I.SpendOzAltin(lot.FixedPrice))
			{
				statusText.text = T("Не хватает OzAltın.", "Not enough OzAltın.", "OzAltın yetmiyor.", "Nicht genug OzAltın.");
				return;
			}
			AddTileToBuyer(lot.TileId);
			lot.Status = AuctionLotStatus.Sold;
			RefreshWindow();
		}
	}

	private void PlaceBid(AuctionLot lot, int amount)
	{
		if (lot != null && lot.Status == AuctionLotStatus.Active && lot.SaleMode == AuctionSaleMode.LiveBid)
		{
			if (!CurrencyService.I.CanSpendOzAltin(amount))
			{
				statusText.text = T("Не хватает OzAltın для ставки.", "Not enough OzAltın for this bid.", "Teklif için OzAltın yetmiyor.", "Nicht genug OzAltın fuer dieses Gebot.");
				return;
			}
			lot.CurrentBid = amount;
			lot.HighestBidderId = GetLocalPlayerId();
			lot.HighestBidderName = GetLocalPlayerName();
			RefreshWindow();
		}
	}

	private void AddTileToBuyer(string tileId)
	{
		MahjongBattleTileInventoryData orCreateInventory = BattleTileInventoryService.GetOrCreateInventory(GetProfile());
		if (orCreateInventory != null)
		{
			if (!orCreateInventory.ActiveTileIds.Contains(tileId) && !orCreateInventory.ReserveTileIds.Contains(tileId))
			{
				orCreateInventory.ReserveTileIds.Insert(0, tileId);
			}
			ProfileService.I?.Save();
			ProfileService.I?.NotifyProfileChanged();
		}
	}

	private void CancelLot(AuctionLot lot)
	{
		if (lot != null && CanCancel(lot))
		{
			lot.Status = AuctionLotStatus.Cancelled;
			ReturnTileToSeller(lot);
			ProfileService.I?.Save();
			ProfileService.I?.NotifyProfileChanged();
			RefreshWindow();
		}
	}

	private bool ResolveExpiredLots()
	{
		bool flag = false;
		for (int i = 0; i < lots.Count; i++)
		{
			AuctionLot auctionLot = lots[i];
			if (auctionLot != null && auctionLot.Status == AuctionLotStatus.Active && auctionLot.SaleMode == AuctionSaleMode.LiveBid && !(Time.realtimeSinceStartup < auctionLot.EndsAtRealtime))
			{
				if (auctionLot.CurrentBid > 0 && !string.IsNullOrWhiteSpace(auctionLot.HighestBidderId))
				{
					auctionLot.Status = AuctionLotStatus.Sold;
				}
				else
				{
					auctionLot.Status = AuctionLotStatus.Expired;
					ReturnTileToSeller(auctionLot);
				}
				flag = true;
			}
		}
		if (flag)
		{
			ProfileService.I?.Save();
			ProfileService.I?.NotifyProfileChanged();
		}
		return flag;
	}

	private bool CanCancel(AuctionLot lot)
	{
		if (lot != null && lot.Status == AuctionLotStatus.Active)
		{
			if (lot.SaleMode != AuctionSaleMode.FixedPrice)
			{
				return lot.CurrentBid <= 0;
			}
			return true;
		}
		return false;
	}

	private int GetNextBid(AuctionLot lot)
	{
		if (lot.CurrentBid > 0)
		{
			return lot.CurrentBid + lot.MinBidStep;
		}
		return lot.StartPrice;
	}

	private static int GetMinBidStep(int price)
	{
		return Mathf.Max(50, Mathf.CeilToInt((float)Mathf.Max(1, price) * 0.08f));
	}

	private bool IsTileListed(string tileId)
	{
		for (int i = 0; i < lots.Count; i++)
		{
			AuctionLot auctionLot = lots[i];
			if (auctionLot != null && auctionLot.Status == AuctionLotStatus.Active && string.Equals(auctionLot.TileId, tileId, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsAuctionSellable(string tileId)
	{
		BattleTileData battleTileData = ResolveTileData(tileId);
		if (battleTileData != null && battleTileData.Rarity >= BattleTileRarity.Rare)
		{
			return !BattleTileInventoryService.IsBaseBattleTile(tileId);
		}
		return false;
	}

	private bool MatchesSellRarityFilter(string tileId)
	{
		if (sellRarityFilter == AuctionRarityFilter.All)
		{
			return true;
		}
		BattleTileData battleTileData = ResolveTileData(tileId);
		if (battleTileData == null)
		{
			return false;
		}
		return sellRarityFilter switch
		{
			AuctionRarityFilter.Rare => battleTileData.Rarity == BattleTileRarity.Rare, 
			AuctionRarityFilter.Epic => battleTileData.Rarity == BattleTileRarity.Epic, 
			AuctionRarityFilter.Legendary => battleTileData.Rarity == BattleTileRarity.Legendary, 
			AuctionRarityFilter.Mythic => battleTileData.Rarity == BattleTileRarity.Mythic, 
			_ => true, 
		};
	}

	private string FormatTimeLeft(AuctionLot lot)
	{
		return Mathf.CeilToInt(Mathf.Max(0f, lot.EndsAtRealtime - Time.realtimeSinceStartup)) + "s";
	}

	private string LocalizeStatus(AuctionLotStatus status)
	{
		return status switch
		{
			AuctionLotStatus.Sold => T("Продан", "Sold", "Satildi", "Verkauft"), 
			AuctionLotStatus.Expired => T("Истек", "Expired", "Bitti", "Abgelaufen"), 
			AuctionLotStatus.Cancelled => T("Отменен", "Cancelled", "İptal", "Abgebrochen"), 
			_ => T("Активен", "Active", "Aktif", "Aktiv"), 
		};
	}

	private string ResolveTileName(string tileId)
	{
		if (BattleTileStore.I != null && BattleTileStore.I.TryGetTileDataById(tileId, out var data) && !string.IsNullOrWhiteSpace(data.DisplayName))
		{
			return data.DisplayName;
		}
		if (!string.IsNullOrWhiteSpace(tileId))
		{
			return tileId;
		}
		return T("Камень", "Stone", "Taş", "Stein");
	}

	private string ResolveTileNameWithRarity(string tileId)
	{
		string text = ResolveTileName(tileId);
		BattleTileData battleTileData = ResolveTileData(tileId);
		if (battleTileData == null || battleTileData.Rarity <= BattleTileRarity.Standard)
		{
			return text;
		}
		return text + "\n" + ResolveRarityName(battleTileData.Rarity);
	}

	private static BattleTileData ResolveTileData(string tileId)
	{
		if (BattleTileStore.I != null && BattleTileStore.I.TryGetTileDataById(tileId, out var data))
		{
			return data;
		}
		return null;
	}

	private static Sprite ResolveTileSprite(BattleTileData data)
	{
		if (data?.Prefab == null)
		{
			return null;
		}
		if (!(data.Prefab.FaceSprite != null))
		{
			return data.Prefab.BackSprite;
		}
		return data.Prefab.FaceSprite;
	}

	private static int GetSuggestedAuctionPrice(string tileId)
	{
		BattleTileData battleTileData = ResolveTileData(tileId);
		if (battleTileData == null)
		{
			return 800;
		}
		return battleTileData.Rarity switch
		{
			BattleTileRarity.Mythic => 45000, 
			BattleTileRarity.Legendary => 12000, 
			BattleTileRarity.Epic => 2500, 
			BattleTileRarity.Rare => 800, 
			BattleTileRarity.Common => 200, 
			_ => 800, 
		};
	}

	private string ResolveRarityName(BattleTileRarity rarity)
	{
		return rarity switch
		{
			BattleTileRarity.Common => T("Обычный", "Common", "Yaygin", "Gewoehnlich"), 
			BattleTileRarity.Rare => T("Редкий", "Rare", "Nadir", "Selten"), 
			BattleTileRarity.Epic => T("Эпический", "Epic", "Epik", "Episch"), 
			BattleTileRarity.Legendary => T("Легендарный", "Legendary", "Efsanevi", "Legendaer"), 
			BattleTileRarity.Mythic => T("Мифический", "Mythic", "Mitik", "Mythisch"), 
			_ => T("Стандарт", "Standard", "Standart", "Standard"), 
		};
	}

	private string FilterLabel(AuctionRarityFilter filter)
	{
		return filter switch
		{
			AuctionRarityFilter.Rare => T("Rare", "Rare", "Nadir", "Rare"), 
			AuctionRarityFilter.Epic => T("Epic", "Epic", "Epik", "Epic"), 
			AuctionRarityFilter.Legendary => T("Legend", "Legend", "Efsane", "Legend"), 
			AuctionRarityFilter.Mythic => T("Mythic", "Mythic", "Mitik", "Mythic"), 
			_ => T("Все", "All", "Hepsi", "Alle"), 
		};
	}

	private string ResolveRarityShortName(BattleTileRarity rarity)
	{
		return rarity switch
		{
			BattleTileRarity.Mythic => T("Миф.", "Myth.", "Mitik", "Myth."), 
			BattleTileRarity.Legendary => T("Лег.", "Leg.", "Efs.", "Leg."), 
			BattleTileRarity.Epic => T("Эпик", "Epic", "Epik", "Episch"), 
			BattleTileRarity.Rare => T("Ред.", "Rare", "Nadir", "Selten"), 
			BattleTileRarity.Common => T("Обыч.", "Common", "Yay.", "Gew."), 
			_ => string.Empty, 
		};
	}

	private string FormatTileInfo(string tileId)
	{
		BattleTileData battleTileData = ResolveTileData(tileId);
		if (battleTileData == null)
		{
			return ResolveTileName(tileId);
		}
		List<string> list = new List<string>
		{
			ResolveTileName(tileId),
			ResolveRarityName(battleTileData.Rarity)
		};
		if (battleTileData.PassiveBonus != null && battleTileData.PassiveBonus.HasAnyBonus())
		{
			list.Add(FormatPassiveBonus(battleTileData.PassiveBonus));
		}
		if (battleTileData.ActiveBonus != null && battleTileData.ActiveBonus.HasAnyBonus())
		{
			list.Add(FormatActiveBonus(battleTileData.ActiveBonus));
		}
		if (!string.IsNullOrWhiteSpace(battleTileData.Skill?.Name))
		{
			list.Add(battleTileData.Skill.Name);
		}
		return string.Join("  |  ", list);
	}

	private static string FormatPassiveBonus(BattleTileBonusData bonus)
	{
		List<string> list = new List<string>();
		if (bonus.MaxHp > 0)
		{
			list.Add("HP +" + bonus.MaxHp);
		}
		if (bonus.Attack > 0)
		{
			list.Add("ATK +" + bonus.Attack);
		}
		if (bonus.Armor > 0f)
		{
			list.Add("ARM +" + Mathf.RoundToInt(bonus.Armor * 100f) + "%");
		}
		if (bonus.CritChance > 0f)
		{
			list.Add("Crit +" + Mathf.RoundToInt(bonus.CritChance * 100f) + "%");
		}
		if (bonus.CritDamageMultiplier > 1f)
		{
			list.Add("CritDmg x" + bonus.CritDamageMultiplier.ToString("0.0"));
		}
		return string.Join(", ", list);
	}

	private static string FormatActiveBonus(BattleTileActiveBonusData bonus)
	{
		List<string> list = new List<string>();
		if (bonus.Attack > 0)
		{
			list.Add("Hit +" + bonus.Attack);
		}
		if (bonus.CritChance > 0f)
		{
			list.Add("HitCrit +" + Mathf.RoundToInt(bonus.CritChance * 100f) + "%");
		}
		if (bonus.CritDamageMultiplier > 1f)
		{
			list.Add("HitCritDmg x" + bonus.CritDamageMultiplier.ToString("0.0"));
		}
		if (bonus.HealSelf > 0)
		{
			list.Add("Heal +" + bonus.HealSelf);
		}
		return string.Join(", ", list);
	}

	private static Color ResolveRarityFrameColor(BattleTileRarity rarity)
	{
		return rarity switch
		{
			BattleTileRarity.Mythic => new Color(0.38f, 0.12f, 0.55f, 0.88f), 
			BattleTileRarity.Legendary => new Color(0.46f, 0.3f, 0.06f, 0.9f), 
			BattleTileRarity.Epic => new Color(0.22f, 0.16f, 0.4f, 0.88f), 
			BattleTileRarity.Rare => new Color(0.1f, 0.22f, 0.36f, 0.88f), 
			BattleTileRarity.Common => new Color(0.12f, 0.2f, 0.15f, 0.86f), 
			_ => new Color(0.08f, 0.07f, 0.05f, 0.74f), 
		};
	}

	private int GetGold()
	{
		EnsureProfileServices();
		if (!(CurrencyService.I != null))
		{
			return 0;
		}
		return CurrencyService.I.GetOzAltin();
	}

	private string GetLocalPlayerId()
	{
		PlayerProfile profile = GetProfile();
		if (profile == null)
		{
			return "local-player";
		}
		return profile.Id;
	}

	private string GetLocalPlayerName()
	{
		PlayerProfile profile = GetProfile();
		if (profile == null || string.IsNullOrWhiteSpace(profile.Name))
		{
			return T("Игрок", "Player", "Oyuncu", "Spieler");
		}
		return profile.Name;
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
		GameObject obj = new GameObject("BattleStoneAuctionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

	private static Canvas FindRuntimeCanvas(Scene scene)
	{
		Canvas[] array = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
		foreach (Canvas canvas in array)
		{
			if (canvas != null && canvas.gameObject.scene == scene && string.Equals(canvas.gameObject.name, "BattleStoneAuctionCanvas", StringComparison.Ordinal))
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
			canvas.sortingOrder = 30040;
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

	private void CreateSelectedStoneSlot(Transform parent)
	{
		GameObject gameObject = CreatePanel(parent, "SelectedStoneSlot", new Vector2(270f, 300f), new Vector2(0f, 120f));
		saleSlotRect = gameObject.transform as RectTransform;
		gameObject.AddComponent<AuctionSaleDropSlot>().Configure(this);
		Image component = gameObject.GetComponent<Image>();
		if (component != null)
		{
			component.color = new Color(0.08f, 0.07f, 0.05f, 0.82f);
		}
		if (string.IsNullOrWhiteSpace(selectedTileId))
		{
			CreateText(gameObject.transform, "EmptySlot", T("Выбери\nкамень", "Choose\nstone", "Taş\nseç", "Stein\nwaehlen"), Vector2.zero, new Vector2(180f, 110f), 34f, TextAlignmentOptions.Center);
			return;
		}
		BattleTileData battleTileData = ResolveTileData(selectedTileId);
		Image image = CreateImage(gameObject.transform, "SelectedStoneFace", new Vector2(0f, 46f), new Vector2(190f, 214f));
		image.sprite = ResolveTileSprite(battleTileData);
		image.enabled = image.sprite != null;
		if (!image.enabled)
		{
			CreateText(gameObject.transform, "SelectedStoneFallback", "?", new Vector2(0f, 46f), new Vector2(110f, 110f), 52f, TextAlignmentOptions.Center);
		}
		CreateText(gameObject.transform, "SelectedStoneName", ResolveTileName(selectedTileId), new Vector2(0f, -104f), new Vector2(230f, 42f), 22f, TextAlignmentOptions.Center);
		CreateText(gameObject.transform, "SelectedStoneRarity", ResolveRarityName(battleTileData?.Rarity ?? BattleTileRarity.Standard), new Vector2(0f, -136f), new Vector2(230f, 34f), 20f, TextAlignmentOptions.Center);
	}

	private void CreateSelectedStoneInfo(Transform parent)
	{
		string value = (string.IsNullOrWhiteSpace(selectedTileId) ? T("Кликни по камню или перетащи его в слот продажи.", "Click a stone or drag it into the sale slot.", "Taşa tikla veya satis slotuna surukle.", "Klicke einen Stein an oder ziehe ihn in den Verkaufsslot.") : FormatTileInfo(selectedTileId));
		CreateText(parent, "SelectedStoneInfo", value, new Vector2(0f, -30f), new Vector2(580f, 86f), 22f, TextAlignmentOptions.Center);
	}

	private Button CreateStoneButton(Transform parent, string objectName, string tileId, Vector2 position, Vector2 size, UnityAction action)
	{
		BattleTileData battleTileData = ResolveTileData(tileId);
		GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = size;
		Image component2 = gameObject.GetComponent<Image>();
		component2.color = ResolveRarityFrameColor(battleTileData?.Rarity ?? BattleTileRarity.Standard);
		component2.raycastTarget = true;
		Button component3 = gameObject.GetComponent<Button>();
		component3.targetGraphic = component2;
		if (action != null)
		{
			component3.onClick.AddListener(action);
		}
		BattlePopupStyle.ApplyButton(component3, preserveCurrentColor: true, keepLabelVisible: false);
		gameObject.AddComponent<AuctionStoneDragSource>().Configure(this, tileId);
		Image image = CreateImage(gameObject.transform, "Face", new Vector2(0f, 16f), new Vector2(size.x * 0.82f, size.y * 0.78f));
		image.sprite = ResolveTileSprite(battleTileData);
		image.enabled = image.sprite != null;
		if (!image.enabled)
		{
			CreateText(gameObject.transform, "FaceFallback", "?", new Vector2(0f, 10f), new Vector2(size.x * 0.7f, size.y * 0.5f), 38f, TextAlignmentOptions.Center);
		}
		CreateText(gameObject.transform, "Rarity", ResolveRarityShortName(battleTileData?.Rarity ?? BattleTileRarity.Standard), new Vector2(0f, (0f - size.y) * 0.39f), new Vector2(size.x * 0.86f, 26f), 18f, TextAlignmentOptions.Center);
		return component3;
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
		Image component2 = obj.GetComponent<Image>();
		component2.color = Color.white;
		component2.preserveAspect = true;
		component2.raycastTarget = false;
		return component2;
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
		CreateText(gameObject.transform, "Label", label, Vector2.zero, size, Mathf.Clamp(size.y * 0.42f, 22f, 34f), TextAlignmentOptions.Center).raycastTarget = false;
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
		BattlePopupStyle.ApplyText(component2);
		return component2;
	}

	private TMP_InputField CreateInput(Transform parent, string objectName, string value, Vector2 position, Vector2 size)
	{
		GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = size;
		Image component2 = gameObject.GetComponent<Image>();
		component2.color = new Color(0.06f, 0.07f, 0.1f, 0.92f);
		component2.raycastTarget = true;
		TMP_Text textComponent = CreateText(gameObject.transform, "Text", value, Vector2.zero, new Vector2(size.x - 34f, size.y - 16f), 30f, TextAlignmentOptions.Center);
		TMP_Text tMP_Text = CreateText(gameObject.transform, "Placeholder", value, Vector2.zero, new Vector2(size.x - 34f, size.y - 16f), 30f, TextAlignmentOptions.Center);
		tMP_Text.color = new Color(1f, 1f, 1f, 0.34f);
		TMP_InputField component3 = gameObject.GetComponent<TMP_InputField>();
		component3.textComponent = textComponent;
		component3.placeholder = tMP_Text;
		component3.text = value;
		component3.contentType = TMP_InputField.ContentType.IntegerNumber;
		component3.characterLimit = 6;
		return component3;
	}

	private void CreateEmptyText(string text)
	{
		CreateText(listRoot, "Empty", text, Vector2.zero, new Vector2(1100f, 120f), 38f, TextAlignmentOptions.Center);
	}

	private static void ClearChildren(Transform parent)
	{
		if (!(parent == null))
		{
			for (int num = parent.childCount - 1; num >= 0; num--)
			{
				UnityEngine.Object.Destroy(parent.GetChild(num).gameObject);
			}
		}
	}

	private static void DestroyRuntimeObject(GameObject obj)
	{
		if (!(obj == null))
		{
			UnityEngine.Object.Destroy(obj);
		}
	}

	private static void SetForgeUiVisible(bool visible)
	{
		SetObjectsByNameVisible("BattleStoneForgeCanvas", visible);
		SetObjectsByNameVisible("ButtonBattleStoneForge", visible);
	}

	private static void SetObjectsByNameVisible(string objectName, bool visible)
	{
		GameObject[] array = Resources.FindObjectsOfTypeAll<GameObject>();
		foreach (GameObject gameObject in array)
		{
			if (!(gameObject == null) && gameObject.scene.IsValid() && string.Equals(gameObject.name, objectName, StringComparison.Ordinal))
			{
				gameObject.SetActive(visible);
			}
		}
	}

	private static int ParsePositiveInt(TMP_InputField input, int fallback)
	{
		if (input != null && int.TryParse(input.text, out var result))
		{
			return Mathf.Max(1, result);
		}
		return Mathf.Max(1, fallback);
	}

	private static void AddUnique(List<string> target, List<string> source)
	{
		if (target == null || source == null)
		{
			return;
		}
		for (int i = 0; i < source.Count; i++)
		{
			string text = source[i];
			if (!string.IsNullOrWhiteSpace(text) && !target.Contains(text))
			{
				target.Add(text);
			}
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
