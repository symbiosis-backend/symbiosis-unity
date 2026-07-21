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
public sealed class BattleLoreTutorialUI : MonoBehaviour
{
	private const string LobbySceneName = "LobbyMahjongBattle";

	private const string BattleSceneName = "GameMahjongBattle";

	private const string HostObjectName = "BattleLoreTutorialUI";

	private const string RuntimeCanvasName = "BattleLoreTutorialCanvas";

	private const string OpenButtonName = "ButtonBattleLoreTutorial";

	private const string OverlayName = "BattleLoreTutorialOverlay";

	private const string VictoryClipResourcePath = "Mahjong/Sounds/game-won";

	private const int RuntimeCanvasSortingOrder = 30044;

	private const float TutorialVictoryVolume = 0.88f;

	private const float TotemTutorialRewardDelaySeconds = 2f;

	private static readonly Vector2 OpenButtonSize = new Vector2(340f, 92f);

	private static AudioClip cachedVictoryClip;

	private Canvas rootCanvas;

	private Button openButton;

	private GameObject overlayRoot;

	private Transform stageListContent;

	private int selectedStage;

	private TMP_Text titleText;

	private TMP_Text statusText;

	private GameObject forgeIntroDimRoot;

	private GameObject forgeIntroRoot;

	private string pendingForgeTutorialTileId = string.Empty;

	private GameObject battleIntroDimRoot;

	private GameObject battleIntroRoot;

	private int pendingBattleTutorialStage;

	private GameObject totemIntroDimRoot;

	private GameObject totemIntroRoot;

	private GameObject totemRewardRoot;

	private Coroutine totemRewardDelayRoutine;

	private string pendingTotemTutorialTileId = string.Empty;

	private AudioSource tutorialAudioSource;

	private bool lastOpenButtonVisible;

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
		BattleLoreTutorialUI[] array = UnityEngine.Object.FindObjectsByType<BattleLoreTutorialUI>(FindObjectsInactive.Include);
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
			GameObject obj = new GameObject("BattleLoreTutorialUI", typeof(RectTransform), typeof(BattleLoreTutorialUI));
			SceneManager.MoveGameObjectToScene(obj, scene);
			obj.transform.SetParent(orCreateRuntimeCanvas.transform, worldPositionStays: false);
			RectTransform rectTransform = obj.transform as RectTransform;
			if (rectTransform != null)
			{
				rectTransform.anchorMin = Vector2.zero;
				rectTransform.anchorMax = Vector2.one;
				rectTransform.offsetMin = Vector2.zero;
				rectTransform.offsetMax = Vector2.zero;
			}
		}
	}

	private void Awake()
	{
		rootCanvas = GetComponentInParent<Canvas>();
		EnsureOpenButton();
	}

	private void OnEnable()
	{
		EnsureOpenButton();
		ApplyOpenButtonLayout();
	}

	private void Update()
	{
		bool shouldShow = ShouldShowOpenButton();
		if (shouldShow != lastOpenButtonVisible)
		{
			ApplyOpenButtonLayout();
		}
	}

	private IEnumerator Start()
	{
		if (BattleLoreTutorialSession.ConsumeOpenOnLobbyReturn())
		{
			yield return null;
			yield return null;
			OpenWindow();
		}
	}

	private void OnDestroy()
	{
		if (totemRewardDelayRoutine != null)
		{
			StopCoroutine(totemRewardDelayRoutine);
			totemRewardDelayRoutine = null;
		}
		openButton = null;
		overlayRoot = null;
		totemRewardRoot = null;
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
				GameObject gameObject = new GameObject("ButtonBattleLoreTutorial", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
				gameObject.transform.SetParent(base.transform, worldPositionStays: false);
				openButton = gameObject.GetComponent<Button>();
				openButton.targetGraphic = gameObject.GetComponent<Image>();
				openButton.onClick.AddListener(OpenWindow);
				BattlePopupStyle.ApplyButton(openButton);
				CreateText(gameObject.transform, "Label", T("Обучение", "Training", "Eğitim", "Training"), Vector2.zero, OpenButtonSize, 34f, TextAlignmentOptions.Center).raycastTarget = false;
				BattlePopupStyle.ApplyBattleLobbyUtilityButton(openButton);
				ApplyOpenButtonLayout();
			}
		}
	}

	private void ApplyOpenButtonLayout()
	{
		if (openButton == null)
		{
			return;
		}
		bool showTrainingButton = ShouldShowOpenButton();
		lastOpenButtonVisible = showTrainingButton;
		if (openButton.gameObject.activeSelf != showTrainingButton)
		{
			openButton.gameObject.SetActive(showTrainingButton);
		}
		if (!showTrainingButton)
		{
			return;
		}
		Vector2 canvasSize = GetCanvasSize(rootCanvas);
		Vector2 baseSize = MainLobbyUiCoordinator.ResolveBattleLobbyBottomButtonSize(canvasSize, new Vector2(390f, 100f));
		Vector2 size = new Vector2(Mathf.Max(baseSize.x * 1.35f, 300f), baseSize.y);
		MainLobbyUiCoordinator.LayoutCenteredButtonSafe(openButton, Vector2.zero, size, canvasSize);
		SetOpenButtonLabelRect(size);
	}

	private static bool ShouldShowOpenButton()
	{
		return !BattleLoreTutorialSession.IsTrainingComplete && !BattleLobbyUiCoordinator.HasModalOpen;
	}

	private void SetOpenButtonLabelRect(Vector2 size)
	{
		TMP_Text label = (openButton != null) ? openButton.GetComponentInChildren<TMP_Text>(includeInactive: true) : null;
		RectTransform rect = (label != null) ? (label.transform as RectTransform) : null;
		if (rect != null)
		{
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
			rect.sizeDelta = size;
		}
		if (label != null)
		{
			label.fontSize = 52f;
			label.fontSizeMax = label.fontSize;
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

	private void OpenWindow()
	{
		BattleLobbyUiCoordinator.OpenModal(BattleLobbyModalKind.LoreTutorial);
		EnsureOverlay();
		selectedStage = GetDefaultCarouselStage();
		overlayRoot.SetActive(value: true);
		overlayRoot.transform.SetAsLastSibling();
		RefreshWindow();
	}

	public static bool TryOpenWindowFromLobby()
	{
		BattleLoreTutorialUI battleLoreTutorialUI = UnityEngine.Object.FindAnyObjectByType<BattleLoreTutorialUI>(FindObjectsInactive.Include);
		if (battleLoreTutorialUI == null)
		{
			return false;
		}
		battleLoreTutorialUI.OpenWindow();
		return true;
	}

	private void CloseWindow()
	{
		ClearBattleIntro();
		ClearForgeIntro();
		ClearTotemIntro();
		if (overlayRoot != null)
		{
			overlayRoot.SetActive(value: false);
		}
		BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.LoreTutorial);
		ApplyOpenButtonLayout();
	}

	private void EnsureOverlay()
	{
		if (!(overlayRoot != null))
		{
			GetOrCreateRuntimeCanvas(base.gameObject.scene);
			overlayRoot = new GameObject("BattleLoreTutorialOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			overlayRoot.transform.SetParent(base.transform, worldPositionStays: false);
			RectTransform component = overlayRoot.GetComponent<RectTransform>();
			component.anchorMin = Vector2.zero;
			component.anchorMax = Vector2.one;
			component.offsetMin = Vector2.zero;
			component.offsetMax = Vector2.zero;
			Image component2 = overlayRoot.GetComponent<Image>();
			component2.color = new Color(0f, 0f, 0f, 0.68f);
			component2.raycastTarget = true;
			GameObject gameObject = CreatePanel(overlayRoot.transform, "LoreTutorialPanel", new Vector2(2180f, 1020f), Vector2.zero);
			titleText = CreateText(gameObject.transform, "Title", string.Empty, new Vector2(0f, 438f), new Vector2(1500f, 70f), 54f, TextAlignmentOptions.Center);
			CreateText(gameObject.transform, "Subtitle", T("5 этапов посвящения: бой, броня, кузница, тотем и FULL экран.", "5 initiation stages: combat, armor, forge, totem, and the FULL screen.", "5 kabul aşaması: savaş, armor, forge, totem ve FULL ekran.", "5 Einweihungsstufen: Kampf, Armor, Forge, Totem und FULL-Bildschirm."), new Vector2(0f, 360f), new Vector2(1720f, 70f), 30f, TextAlignmentOptions.Center);
			stageListContent = CreateCarouselRoot(gameObject.transform, "StageCarousel", new Vector2(1900f, 660f), new Vector2(0f, -34f));
			statusText = CreateText(gameObject.transform, "Status", string.Empty, new Vector2(0f, -430f), new Vector2(1600f, 54f), 30f, TextAlignmentOptions.Center);
			CreateButton(gameObject.transform, "ButtonCloseLoreTutorial", T("Закрыть", "Close", "Kapat", "Schliessen"), new Vector2(982f, 430f), new Vector2(112f, 112f), CloseWindow);
			overlayRoot.SetActive(value: false);
		}
	}

	private void RefreshWindow()
	{
		if (titleText != null)
		{
			titleText.text = T("Обучение: Сгоревший Бамбуковый Лес", "Training: Burned Bamboo Forest", "Eğitim: Yanmış Bambu Ormanı", "Training: Verbrannter Bambuswald");
		}
		if (statusText != null)
		{
			statusText.text = (BattleLoreTutorialSession.RewardClaimed ? T("Посвящение завершено. Стартовые редкий и эпический камни уже выбраны.", "Initiation complete. The starter rare and epic stones are already chosen.", "Kabul tamamlandı. Başlangıç rare ve epic taşları seçildi.", "Einweihung abgeschlossen. Rare und epic Startsteine sind gewaehlt.") : T("Каждый этап дает мелкую награду. Финал открывает выбор первых редкого и эпического камней.", "Every stage gives a small reward. The finale unlocks your first rare and epic stone choices.", "Her aşama küçük ödül verir. Final ilk rare ve epic taş seçimini açar.", "Jede Stufe gibt eine kleine Belohnung. Das Finale oeffnet rare und epic Startsteine."));
		}
		RebuildStages();
	}

	private void RebuildStages()
	{
		if (!(stageListContent == null))
		{
			ClearDynamicChildren(stageListContent);
			int completedStage = BattleLoreTutorialSession.CompletedStage;
			int num = BattleLoreTutorialSession.GetNextPlayableStage(Mathf.Clamp(completedStage + 1, 1, 6));
			selectedStage = BattleLoreTutorialSession.NormalizePlayableStage(Mathf.Clamp((selectedStage <= 0) ? num : selectedStage, 1, num));
			bool unlocked = selectedStage <= completedStage + 1;
			bool complete = selectedStage <= completedStage;
			CreateStageBlock(stageListContent, selectedStage, unlocked, complete, Vector2.zero);
			CreateCarouselButton(stageListContent, "ButtonPrevStage", "<", new Vector2(-840f, -4f), delegate
			{
				ShiftSelectedStage(-1);
			}, selectedStage > 1);
			CreateCarouselButton(stageListContent, "ButtonNextStage", ">", new Vector2(840f, -4f), delegate
			{
				ShiftSelectedStage(1);
			}, selectedStage < num);
		}
	}

	private void CreateStageBlock(Transform parent, int stage, bool unlocked, bool complete, Vector2 position)
	{
		int displayStage = BattleLoreTutorialSession.GetDisplayStageNumber(stage);
		GameObject obj = CreateThinPanel(parent, $"TrainingStage_{stage:00}", new Vector2(1500f, 590f), position);
		obj.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = obj.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = new Vector2(1500f, 590f);
		Image component2 = obj.GetComponent<Image>();
		component2.color = (complete ? new Color(0.115f, 0.07f, 0.03f, 0.94f) : (unlocked ? new Color(0.07f, 0.048f, 0.03f, 0.95f) : new Color(0.055f, 0.05f, 0.045f, 0.84f)));
		component2.raycastTarget = true;
		string label = (complete ? T("Пройдено", "Done", "Tamam", "Fertig") : (unlocked ? T("Начать", "Start", "Başla", "Start") : T("Закрыто", "Locked", "Kilitli", "Gesperrt")));
		string value = ((stage == 4) ? T("3 камня", "3 stones", "3 taş", "3 Steine") : ((stage == 5) ? T("Сумка", "Bag", "Çanta", "Tasche") : ((stage == 6) ? "FULL" : (BattleLoreTutorialSession.GetTutorialTileCount(stage) + T(" тайлов", " tiles", " taş", " Steine")))));
		CreateText(obj.transform, "StageCounter", $"{displayStage}/{BattleLoreTutorialSession.PlayableStageCount}", new Vector2(592f, 228f), new Vector2(200f, 52f), 38f, TextAlignmentOptions.Center);
		CreateText(obj.transform, "StageSize", value, new Vector2(592f, 174f), new Vector2(260f, 52f), 34f, TextAlignmentOptions.Center);
		CreateText(obj.transform, "StageTitle", $"{displayStage}. {BattleLoreTutorialSession.GetStageTitle(stage)}", new Vector2(0f, 200f), new Vector2(1040f, 86f), 62f, TextAlignmentOptions.Center);
		CreateText(obj.transform, "StageLesson", BattleLoreTutorialSession.GetStageLesson(stage), new Vector2(0f, 38f), new Vector2(1220f, 220f), 47f, TextAlignmentOptions.Center);
		CreateText(obj.transform, "StageReward", T("Награда: ", "Reward: ", "Ödül: ", "Belohnung: ") + BattleLoreTutorialSession.GetStageRewardText(stage), new Vector2(0f, -152f), new Vector2(1160f, 78f), 42f, TextAlignmentOptions.Center);
		UnityAction action = null;
		if (unlocked && !complete)
		{
			action = delegate
			{
				StartStage(stage);
			};
		}
		CreateButton(obj.transform, "ButtonStartStage", label, new Vector2(0f, -246f), new Vector2(430f, 104f), action).interactable = unlocked && !complete;
	}

	private void CreateCarouselButton(Transform parent, string objectName, string label, Vector2 position, UnityAction action, bool interactable)
	{
		CreateButton(parent, objectName, label, position, new Vector2(120f, 160f), action).interactable = interactable;
	}

	private void ShiftSelectedStage(int delta)
	{
		int max = BattleLoreTutorialSession.GetNextPlayableStage(Mathf.Clamp(BattleLoreTutorialSession.CompletedStage + 1, 1, 6));
		int next = Mathf.Clamp(selectedStage + delta, 1, max);
		selectedStage = delta < 0
			? BattleLoreTutorialSession.GetPreviousPlayableStage(next)
			: BattleLoreTutorialSession.GetNextPlayableStage(next);
		RefreshWindow();
	}

	private static int GetDefaultCarouselStage()
	{
		return BattleLoreTutorialSession.GetNextPlayableStage(Mathf.Clamp(BattleLoreTutorialSession.CompletedStage + 1, 1, 6));
	}

	private void StartStage(int stage)
	{
		stage = BattleLoreTutorialSession.NormalizePlayableStage(stage);
		switch (stage)
		{
		case 4:
			StartLobbyForgeStage();
			return;
		case 5:
			StartLobbyTotemStage();
			return;
		}
		BattleLoreTutorialSession.BeginStage(stage);
		pendingBattleTutorialStage = stage;
		ShowBattleLessonIntro(stage);
	}

	private void ShowBattleLessonIntro(int stage)
	{
		EnsureOverlay();
		if (overlayRoot != null)
		{
			overlayRoot.SetActive(value: true);
			overlayRoot.transform.SetAsLastSibling();
		}
		ClearBattleIntro();
		battleIntroDimRoot = CreateFullscreenDim(overlayRoot.transform, "BattleLessonIntroDim", Color.black);
		battleIntroRoot = CreatePanel(overlayRoot.transform, "BattleLessonIntro", new Vector2(1320f, 700f), Vector2.zero);
		battleIntroDimRoot.transform.SetAsLastSibling();
		battleIntroRoot.transform.SetAsLastSibling();
		CreateText(battleIntroRoot.transform, "Title", GetBattleIntroTitle(stage), new Vector2(0f, 250f), new Vector2(1040f, 72f), 50f, TextAlignmentOptions.Center);
		CreateText(battleIntroRoot.transform, "Body", GetBattleIntroBody(stage), new Vector2(0f, 34f), new Vector2(1080f, 330f), 34f, TextAlignmentOptions.Center);
		CreateText(battleIntroRoot.transform, "Hint", GetBattleIntroHint(stage), new Vector2(0f, -156f), new Vector2(980f, 76f), 30f, TextAlignmentOptions.Center);
		CreateButton(battleIntroRoot.transform, "ButtonStartBattleLesson", T("Начать бой", "Start Battle", "Savaşı Başlat", "Kampf starten"), new Vector2(0f, -264f), new Vector2(430f, 90f), OpenBattleAfterIntro);
	}

	private void OpenBattleAfterIntro()
	{
		if (!BattleTotemRequirementUI.EnsureBattleReady())
			return;

		int stage = Mathf.Clamp(pendingBattleTutorialStage, 1, 6);
		ClearBattleIntro();
		CloseWindow();
		MahjongBattleLobbySession.SetMode(MahjongBattleLobbyMode.RandomMatch);
		MahjongSession.StartBattle(CreateTutorialOpponent(stage), 0, 81000 + stage);
		SceneManager.LoadScene("GameMahjongBattle");
	}

	private void ClearBattleIntro()
	{
		if (battleIntroDimRoot != null)
		{
			UnityEngine.Object.Destroy(battleIntroDimRoot);
			battleIntroDimRoot = null;
		}
		if (battleIntroRoot != null)
		{
			UnityEngine.Object.Destroy(battleIntroRoot);
			battleIntroRoot = null;
		}
	}

	private static string GetBattleIntroTitle(int stage)
	{
		stage = BattleLoreTutorialSession.NormalizePlayableStage(stage);
		int displayStage = BattleLoreTutorialSession.GetDisplayStageNumber(stage);
		return T("Урок " + displayStage + ": " + BattleLoreTutorialSession.GetStageTitle(stage), "Lesson " + displayStage + ": " + BattleLoreTutorialSession.GetStageTitle(stage), displayStage + ". Ders: " + BattleLoreTutorialSession.GetStageTitle(stage), "Lektion " + displayStage + ": " + BattleLoreTutorialSession.GetStageTitle(stage));
	}

	private static string GetBattleIntroBody(int stage)
	{
		return BattleLoreTutorialSession.NormalizePlayableStage(stage) switch
		{
			1 => T("Сейчас откроется короткий учебный бой.\n\nНа поле будут только несколько пар. Первая найденная пара покажет, как удар снимает HP, вторая завершит бой.\n\nСмотри на HP-бары и подсказки уронa.", "A short training battle will open now.\n\nOnly a few pairs will be on the board. The first pair shows how a hit removes HP; the second finishes the fight.\n\nWatch the HP bars and damage hints.", "Kısa bir eğitim savaşı açılacak.\n\nTahtada sadece birkaç eş olacak. İlk eş vuruşun HP düşürdüğünü gösterir, ikinci eş savaşı bitirir.\n\nHP barlarına ve hasar ipuçlarına bak.", "Ein kurzer Trainingskampf startet.\n\nAuf dem Brett liegen nur wenige Paare. Das erste Paar zeigt HP-Schaden, das zweite beendet den Kampf.\n\nAchte auf HP-Leisten und Schadenshinweise."),
			3 => T("Этот бой показывает armor и critical.\n\nArmor режет входящий урон, а critical умножает удар. На всплывающих цифрах будет видно, почему итоговый урон меняется.\n\nПобеда даст OzTile и подготовит редкие камни для кузницы.", "This battle teaches armor and critical hits.\n\nArmor cuts incoming damage, while critical multiplies a hit. Floating numbers show why final damage changes.\n\nWinning gives OzTile and prepares rare stones for Forge.", "Bu savaş armor ve critical öğretir.\n\nArmor gelen hasarı azaltır, critical vuruşu çarpar. Uçan sayılar nihai hasarın neden değiştiğini gösterir.\n\nGalibiyet OzTile verir ve Forge için rare taş hazırlar.", "Dieser Kampf zeigt Armor und Critical.\n\nArmor senkt Schaden, Critical verstaerkt Treffer. Zahlen zeigen den finalen Schaden.\n\nDer Sieg gibt OzTile und bereitet Rare-Steine fuer Forge vor."),
			6 => T("Финальный бой учит FULL экран.\n\nВ бою нажми FULL, чтобы развернуть доску и проверить крупный вид. После этого заверши бой.\n\nПобеда завершит обучение и откроет игровые режимы.", "The final battle teaches the FULL screen.\n\nPress FULL in battle to expand the board and inspect the large view. Then finish the fight.\n\nWinning completes training and unlocks game modes.", "Final savaş FULL ekranı öğretir.\n\nSavaşta FULL'a bas, tahtayı büyüt ve büyük görünümü kontrol et. Sonra savaşı bitir.\n\nGalibiyet eğitimi bitirir ve oyun modlarını açar.", "Der finale Kampf zeigt FULL-Bildschirm.\n\nDruecke im Kampf FULL, pruefe die grosse Ansicht und beende den Kampf.\n\nDer Sieg schliesst Training ab und oeffnet Spielmodi."),
			_ => BattleLoreTutorialSession.GetStageLesson(stage)
		};
	}

	private static string GetBattleIntroHint(int stage)
	{
		return BattleLoreTutorialSession.NormalizePlayableStage(stage) switch
		{
			1 => T("Цель: найти пары и увидеть первый урон.", "Goal: match pairs and see the first damage.", "Hedef: eşleri bul ve ilk hasarı gör.", "Ziel: Paare finden und ersten Schaden sehen."),
			3 => T("Цель: понять armor, crit и награду редких камней.", "Goal: understand armor, crit, and rare-stone reward.", "Hedef: armor, crit ve rare taş ödülünü anlamak.", "Ziel: Armor, Crit und Rare-Belohnung verstehen."),
			6 => T("Цель: нажать FULL и завершить посвящение.", "Goal: press FULL and complete initiation.", "Hedef: FULL'a bas ve eğitimi tamamla.", "Ziel: FULL druecken und Einweihung abschliessen."),
			_ => string.Empty
		};
	}

	private void StartLobbyForgeStage()
	{
		PlayerProfile playerProfile = ProfileRuntimeBootstrap.TryGetProfile();
		BattleTileStore battleTileStore = ((BattleTileStore.I != null) ? BattleTileStore.I : UnityEngine.Object.FindAnyObjectByType<BattleTileStore>());
		if (playerProfile == null || battleTileStore == null)
		{
			if (statusText != null)
			{
				statusText.text = T("Профиль или камни еще загружаются. Попробуй снова через секунду.", "Profile or stones are still loading. Try again in a moment.", "Profil veya taşlar yükleniyor. Birazdan tekrar dene.", "Profil oder Steine laden noch. Versuche es gleich erneut.");
			}
			return;
		}
		string value = PrepareForgeTutorialTile(playerProfile, battleTileStore);
		if (string.IsNullOrWhiteSpace(value))
		{
			if (statusText != null)
			{
				statusText.text = T("Не нашел подходящий редкий камень для урока кузницы.", "No suitable rare stone found for the Forge lesson.", "Forge dersi için uygun rare taş bulunamadı.", "Kein passender Rare-Stein fuer die Forge-Lektion gefunden.");
			}
		}
		else
		{
			BattleLoreTutorialSession.BeginStage(4);
			ProfileService.I?.Save();
			ProfileService.I?.NotifyProfileChanged();
			pendingForgeTutorialTileId = value;
			ShowForgeLessonIntro();
		}
	}

	private void ShowForgeLessonIntro()
	{
		EnsureOverlay();
		ClearForgeIntro();
		forgeIntroDimRoot = CreateFullscreenDim(overlayRoot.transform, "ForgeLessonIntroDim", Color.black);
		forgeIntroRoot = CreatePanel(overlayRoot.transform, "ForgeLessonIntro", new Vector2(1280f, 660f), Vector2.zero);
		forgeIntroDimRoot.transform.SetAsLastSibling();
		forgeIntroRoot.transform.SetAsLastSibling();
		CreateText(forgeIntroRoot.transform, "Title", GetBattleIntroTitle(4), new Vector2(0f, 234f), new Vector2(980f, 72f), 50f, TextAlignmentOptions.Center);
		CreateText(forgeIntroRoot.transform, "Body", T("Сейчас откроется кузница. Слоты справа сначала пустые.\n\nВыбери редкий камень из списка слева: три его копии заполнят слоты. Затем нажми «Соединить».\n\nТри одинаковых камня слетятся в один, вспыхнут и станут улучшенным камнем +1.", "Forge will open now. The slots on the right start empty.\n\nChoose a rare stone from the list on the left: its three copies will fill the slots. Then press Combine.\n\nThree identical stones will fly into one, flash, and become a +1 upgraded stone.", "Şimdi Forge açılacak. Sağdaki yuvalar önce boş olacak.\n\nSoldaki listeden rare taşı seç: üç kopyası yuvaları dolduracak. Sonra Birleştir'e bas.\n\nÜç aynı taş tek taşa uçar, ışık patlar ve +1 güçlendirilmiş taş kalır.", "Forge wird jetzt geoeffnet. Die rechten Slots sind zuerst leer.\n\nWaehle links einen Rare-Stein: seine drei Kopien fuellen die Slots. Dann druecke Verbinden.\n\nDrei gleiche Steine fliegen zusammen, blitzen auf und werden zu einem +1 Stein."), new Vector2(0f, 26f), new Vector2(1040f, 300f), 32f, TextAlignmentOptions.Center);
		CreateButton(forgeIntroRoot.transform, "ButtonOpenForgeLesson", T("Понятно", "OK", "Tamam", "OK"), new Vector2(0f, -238f), new Vector2(360f, 86f), OpenForgeAfterIntro);
	}

	private void OpenForgeAfterIntro()
	{
		ClearForgeIntro();
		if (!BattleStoneForgeUI.TryOpenTutorialForge(pendingForgeTutorialTileId))
		{
			BattleLoreTutorialSession.ClearActive();
			if (statusText != null)
			{
				statusText.text = T("Окно кузницы еще не готово. Открой урок снова.", "Forge window is not ready yet. Open the lesson again.", "Forge penceresi hazır değil. Dersi tekrar aç.", "Forge-Fenster ist noch nicht bereit. Oeffne die Lektion erneut.");
			}
		}
		else
		{
			CloseWindow();
		}
	}

	private void ClearForgeIntro()
	{
		if (forgeIntroDimRoot != null)
		{
			UnityEngine.Object.Destroy(forgeIntroDimRoot);
			forgeIntroDimRoot = null;
		}
		if (forgeIntroRoot != null)
		{
			UnityEngine.Object.Destroy(forgeIntroRoot);
			forgeIntroRoot = null;
		}
	}

	private void StartLobbyTotemStage()
	{
		PlayerProfile playerProfile = ProfileRuntimeBootstrap.TryGetProfile();
		BattleTileStore battleTileStore = ((BattleTileStore.I != null) ? BattleTileStore.I : UnityEngine.Object.FindAnyObjectByType<BattleTileStore>());
		if (playerProfile == null || battleTileStore == null)
		{
			if (statusText != null)
			{
				statusText.text = T("Профиль или камни еще загружаются. Попробуй снова через секунду.", "Profile or stones are still loading. Try again in a moment.", "Profil veya taşlar yükleniyor. Birazdan tekrar dene.", "Profil oder Steine laden noch. Versuche es gleich erneut.");
			}
			return;
		}
		string value = PrepareTotemTutorialTile(playerProfile, battleTileStore);
		if (string.IsNullOrWhiteSpace(value))
		{
			if (statusText != null)
			{
				statusText.text = T("Не нашел подходящий камень для урока тотема.", "No suitable stone found for the Totem lesson.", "Totem dersi için uygun taş bulunamadı.", "Kein passender Stein fuer die Totem-Lektion gefunden.");
			}
		}
		else
		{
			BattleLoreTutorialSession.BeginStage(5);
			ProfileService.I?.Save();
			ProfileService.I?.NotifyProfileChanged();
			pendingTotemTutorialTileId = value;
			ShowTotemLessonIntro();
		}
	}

	private void ShowTotemLessonIntro()
	{
		EnsureOverlay();
		if (overlayRoot != null)
		{
			overlayRoot.SetActive(value: true);
			overlayRoot.transform.SetAsLastSibling();
		}
		ClearTotemIntro();
		totemIntroDimRoot = CreateFullscreenDim(overlayRoot.transform, "TotemLessonIntroDim", Color.black);
		totemIntroRoot = CreatePanel(overlayRoot.transform, "TotemLessonIntro", new Vector2(1280f, 660f), Vector2.zero);
		totemIntroDimRoot.transform.SetAsLastSibling();
		totemIntroRoot.transform.SetAsLastSibling();
		CreateText(totemIntroRoot.transform, "Title", GetBattleIntroTitle(5), new Vector2(0f, 234f), new Vector2(980f, 72f), 50f, TextAlignmentOptions.Center);
		CreateText(totemIntroRoot.transform, "Body", T("Сейчас откроется сумка.\n\nВ блоке активных камней зажми один камень и перетащи его в верхний слот тотема слева.\n\nКамень останется среди 18 активных, характеристики героя обновятся, а урок завершится.", "The Bag will open now.\n\nHold one stone in the Active block and drag it into the Totem slot at the upper left.\n\nThe stone remains among the 18 active stones, the hero stats update, and the lesson completes.", "Şimdi Çanta açılacak.\n\nAktif taşlar bölümünde bir taşı basılı tutup sol üstteki Totem yuvasına sürükle.\n\nTaş 18 aktif taş arasında kalır, kahraman özellikleri güncellenir ve ders tamamlanır.", "Die Tasche wird jetzt geöffnet.\n\nHalte einen Stein im Bereich Aktive Steine gedrückt und ziehe ihn links oben in den Totem-Slot.\n\nDer Stein bleibt unter den 18 aktiven Steinen, die Heldenwerte werden aktualisiert und die Lektion endet."), new Vector2(0f, 20f), new Vector2(1040f, 318f), 32f, TextAlignmentOptions.Center);
		CreateButton(totemIntroRoot.transform, "ButtonOpenTotemLesson", T("Понятно", "OK", "Tamam", "OK"), new Vector2(0f, -238f), new Vector2(360f, 86f), OpenBagAfterTotemIntro);
	}

	private void OpenBagAfterTotemIntro()
	{
		ClearTotemIntro();
		CloseWindow();
		if (!BattleLobbyUI.TryOpenBattleTileInventoryForTutorial())
		{
			BattleLoreTutorialSession.ClearActive();
			if (statusText != null)
			{
				statusText.text = T("Сумка еще не готова. Открой урок снова.", "The Bag is not ready yet. Open the lesson again.", "Çanta hazır değil. Dersi tekrar aç.", "Die Tasche ist noch nicht bereit. Oeffne die Lektion erneut.");
			}
		}
	}

	private void ClearTotemIntro()
	{
		if (totemIntroDimRoot != null)
		{
			UnityEngine.Object.Destroy(totemIntroDimRoot);
			totemIntroDimRoot = null;
		}
		if (totemIntroRoot != null)
		{
			UnityEngine.Object.Destroy(totemIntroRoot);
			totemIntroRoot = null;
		}
	}

	public static void NotifyTotemEquippedFromLobby(string tileId)
	{
		if (BattleLoreTutorialSession.IsActive && BattleLoreTutorialSession.ActiveStage == 5)
		{
			BattleLoreTutorialUI battleLoreTutorialUI = UnityEngine.Object.FindAnyObjectByType<BattleLoreTutorialUI>(FindObjectsInactive.Include);
			if (battleLoreTutorialUI != null)
			{
				battleLoreTutorialUI.ShowTotemTutorialRewardResultDelayed(tileId);
				return;
			}
			BattleLoreTutorialSession.GrantStageReward(5);
			BattleLoreTutorialSession.CompleteActiveStage();
			BattleLoreTutorialSession.ClearActive();
		}
	}

	private void ShowTotemTutorialRewardResultDelayed(string tileId)
	{
		if (totemRewardDelayRoutine != null)
		{
			StopCoroutine(totemRewardDelayRoutine);
		}
		totemRewardDelayRoutine = StartCoroutine(ShowTotemTutorialRewardResultAfterDelay(tileId));
	}

	private IEnumerator ShowTotemTutorialRewardResultAfterDelay(string tileId)
	{
		yield return new WaitForSecondsRealtime(TotemTutorialRewardDelaySeconds);
		totemRewardDelayRoutine = null;
		ShowTotemTutorialRewardResult(tileId);
	}

	private void ShowTotemTutorialRewardResult(string tileId)
	{
		if (!BattleLoreTutorialSession.IsActive || BattleLoreTutorialSession.ActiveStage != 5)
		{
			return;
		}
		if (totemRewardRoot != null)
		{
			UnityEngine.Object.Destroy(totemRewardRoot);
		}
		BattleLoreTutorialSession.GrantStageReward(5);
		BattleLoreTutorialSession.CompleteActiveStage();
		BattleLoreTutorialSession.ClearActive();
		if (GetOrCreateRuntimeCanvas(base.gameObject.scene) == null)
		{
			return;
		}
		totemRewardRoot = new GameObject("TotemTutorialRewardOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		totemRewardRoot.transform.SetParent(base.transform, worldPositionStays: false);
		totemRewardRoot.transform.SetAsLastSibling();
		RectTransform component = totemRewardRoot.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		Image component2 = totemRewardRoot.GetComponent<Image>();
		component2.color = Color.black;
		component2.raycastTarget = true;
		PlayVictorySound();
		int stageOzTileReward = BattleLoreTutorialSession.GetStageOzTileReward(5);
		GameObject obj = CreatePanel(totemRewardRoot.transform, "RewardPanel", new Vector2(1120f, 620f), Vector2.zero);
		CreateText(obj.transform, "Title", T("Урок завершен", "Lesson Complete", "Ders Tamamlandı", "Lektion abgeschlossen"), new Vector2(0f, 214f), new Vector2(860f, 70f), 54f, TextAlignmentOptions.Center);
		CreateText(obj.transform, "Body", T("Тотем готов. Этот камень остаётся одним из 18 активных и одновременно усиливает героя.", "Totem ready. This stone remains one of the 18 active stones and also empowers the hero.", "Totem hazır. Bu taş 18 aktif taştan biri olarak kalır ve aynı zamanda kahramanı güçlendirir.", "Totem bereit. Dieser Stein bleibt einer der 18 aktiven Steine und stärkt zugleich den Helden."), new Vector2(0f, 56f), new Vector2(860f, 140f), 34f, TextAlignmentOptions.Center);
		CreateText(obj.transform, "Reward", T("Награда", "Reward", "Ödül", "Belohnung") + $": +{stageOzTileReward} OzTile", new Vector2(0f, -92f), new Vector2(760f, 70f), 42f, TextAlignmentOptions.Center);
		CreateButton(obj.transform, "ButtonCloseReward", T("Забрать", "Claim", "Al", "Nehmen"), new Vector2(0f, -232f), new Vector2(420f, 90f), delegate
		{
			if (totemRewardRoot != null)
			{
				UnityEngine.Object.Destroy(totemRewardRoot);
				totemRewardRoot = null;
			}
			BattleLobbyUI.CloseBattleTileInventoryForTutorial();
			TryOpenWindowFromLobby();
		});
	}

	private static string PrepareForgeTutorialTile(PlayerProfile profile, BattleTileStore store)
	{
		BattleTileInventoryService.EnsureInventoryForStore(profile, store);
		IReadOnlyList<BattleTileData> readOnlyList = ((store != null) ? store.BattleTiles : null);
		if (readOnlyList == null)
		{
			return string.Empty;
		}
		BattleTileData battleTileData = null;
		string stageThreeRewardTileId = BattleLoreTutorialSession.GetStageThreeRewardTileId();
		if (!string.IsNullOrWhiteSpace(stageThreeRewardTileId) && store.TryGetTileDataById(stageThreeRewardTileId, out var data) && data != null && data.Prefab != null && data.Rarity == BattleTileRarity.Rare)
		{
			battleTileData = data;
		}
		for (int i = 0; i < readOnlyList.Count; i++)
		{
			if (battleTileData != null)
			{
				break;
			}
			BattleTileData battleTileData2 = readOnlyList[i];
			if (battleTileData2 != null && !(battleTileData2.Prefab == null) && !string.IsNullOrWhiteSpace(battleTileData2.Id) && battleTileData2.Rarity == BattleTileRarity.Rare)
			{
				battleTileData = battleTileData2;
				break;
			}
		}
		if (battleTileData == null)
		{
			return string.Empty;
		}
		for (int j = BattleTileInventoryService.GetOwnedCount(profile, battleTileData.Id, 0); j < 3; j++)
		{
			BattleTileInventoryService.GrantTileCopy(profile, store, battleTileData.Id, out var _);
		}
		return battleTileData.Id;
	}

	private static string PrepareTotemTutorialTile(PlayerProfile profile, BattleTileStore store)
	{
		BattleTileInventoryService.EnsureInventoryForStore(profile, store);
		MahjongBattleTileInventoryData orCreateInventory = BattleTileInventoryService.GetOrCreateInventory(profile);
		IReadOnlyList<BattleTileData> readOnlyList = ((store != null) ? store.BattleTiles : null);
		if (orCreateInventory == null || readOnlyList == null)
		{
			return string.Empty;
		}
		string reason;
		if (!string.IsNullOrWhiteSpace(orCreateInventory.TotemTileId))
		{
			BattleTileInventoryService.TryClearTotemTile(profile, store, out reason);
		}
		BattleTileData battleTileData = null;
		for (int i = 0; i < 3; i++)
		{
			if (battleTileData != null)
			{
				break;
			}
			for (int j = 0; j < readOnlyList.Count; j++)
			{
				BattleTileData battleTileData2 = readOnlyList[j];
				if (battleTileData2 != null && !(battleTileData2.Prefab == null) && !string.IsNullOrWhiteSpace(battleTileData2.Id) && !BattleTileInventoryService.IsBaseBattleTile(battleTileData2.Id))
				{
					bool flag = battleTileData2.Skill != null && battleTileData2.Skill.HasSkill();
					bool flag2 = battleTileData2.Rarity >= BattleTileRarity.Rare;
					if ((i != 0 || (flag2 && flag)) && (i != 1 || flag2))
					{
						battleTileData = battleTileData2;
						break;
					}
				}
			}
		}
		if (battleTileData == null)
		{
			return string.Empty;
		}
		if (BattleTileInventoryService.GetOwnedCount(profile, battleTileData.Id) <= 0)
		{
			BattleTileInventoryService.GrantTileCopy(profile, store, battleTileData.Id, out var _);
		}
		if (orCreateInventory.ActiveTileIds.Contains(battleTileData.Id))
		{
			BattleTileInventoryService.TryReserveTile(profile, store, battleTileData.Id, out reason);
		}
		else if (!orCreateInventory.ReserveTileIds.Contains(battleTileData.Id))
		{
			orCreateInventory.ReserveTileIds.Insert(0, battleTileData.Id);
		}
		orCreateInventory.EnsureValid();
		return battleTileData.Id;
	}

	private void PlayVictorySound()
	{
		if (AppSettings.I != null && !AppSettings.I.SoundEnabled)
		{
			return;
		}
		AudioClip audioClip = ResolveVictoryClip();
		if (!(audioClip == null))
		{
			EnsureTutorialAudioSource();
			if (tutorialAudioSource != null)
			{
				tutorialAudioSource.PlayOneShot(audioClip, 0.88f);
			}
		}
	}

	private void EnsureTutorialAudioSource()
	{
		if (!(tutorialAudioSource != null))
		{
			tutorialAudioSource = GetComponent<AudioSource>();
			if (tutorialAudioSource == null)
			{
				tutorialAudioSource = base.gameObject.AddComponent<AudioSource>();
			}
			tutorialAudioSource.playOnAwake = false;
			tutorialAudioSource.loop = false;
			tutorialAudioSource.spatialBlend = 0f;
		}
	}

	private static AudioClip ResolveVictoryClip()
	{
		if (cachedVictoryClip == null)
		{
			cachedVictoryClip = Resources.Load<AudioClip>("Mahjong/Sounds/game-won");
		}
		return cachedVictoryClip;
	}

	private static MahjongBattleOpponentData CreateTutorialOpponent(int stage)
	{
		return new MahjongBattleOpponentData
		{
			Id = $"battle_lore_tutorial_{Mathf.Clamp(stage, 1, 6):00}",
			DisplayName = T("Эхо пепла", "Ash Echo", "Kül Yankısı", "Ascheecho"),
			RankTier = "Tutorial",
			Level = Mathf.Max(1, stage),
			IsBot = true,
			DifficultyFactor = Mathf.Lerp(0.48f, 0.72f, (float)(Mathf.Clamp(stage, 1, 6) - 1) / 9f),
			StatusLine = BattleLoreTutorialSession.GetStageTitle(stage)
		};
	}

	private static Canvas GetOrCreateRuntimeCanvas(Scene scene)
	{
		Canvas[] array = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
		foreach (Canvas canvas in array)
		{
			if (canvas != null && canvas.gameObject.scene == scene && string.Equals(canvas.name, "BattleLoreTutorialCanvas", StringComparison.Ordinal))
			{
				return canvas;
			}
		}
		GameObject obj = new GameObject("BattleLoreTutorialCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		SceneManager.MoveGameObjectToScene(obj, scene);
		Canvas component = obj.GetComponent<Canvas>();
		component.renderMode = RenderMode.ScreenSpaceOverlay;
		component.overrideSorting = true;
		component.sortingOrder = 30044;
		CanvasScaler component2 = obj.GetComponent<CanvasScaler>();
		component2.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		component2.referenceResolution = new Vector2(2400f, 1080f);
		component2.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
		component2.matchWidthOrHeight = 0.5f;
		return component;
	}

	private static Transform CreateScrollContent(Transform parent, string objectName, Vector2 position, Vector2 size)
	{
		GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = size;
		gameObject.GetComponent<Image>().color = new Color(0.03f, 0.025f, 0.02f, 0.42f);
		GameObject gameObject2 = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
		gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
		RectTransform component2 = gameObject2.GetComponent<RectTransform>();
		component2.anchorMin = Vector2.zero;
		component2.anchorMax = Vector2.one;
		component2.offsetMin = Vector2.zero;
		component2.offsetMax = Vector2.zero;
		gameObject2.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
		gameObject2.GetComponent<Mask>().showMaskGraphic = false;
		GameObject obj = new GameObject("Content", typeof(RectTransform));
		obj.transform.SetParent(gameObject2.transform, worldPositionStays: false);
		RectTransform component3 = obj.GetComponent<RectTransform>();
		component3.anchorMin = new Vector2(0.5f, 1f);
		component3.anchorMax = new Vector2(0.5f, 1f);
		component3.pivot = new Vector2(0.5f, 1f);
		component3.anchoredPosition = Vector2.zero;
		component3.sizeDelta = new Vector2(size.x, 1320f);
		ScrollRect component4 = gameObject.GetComponent<ScrollRect>();
		component4.viewport = component2;
		component4.content = component3;
		component4.horizontal = false;
		component4.vertical = true;
		component4.movementType = ScrollRect.MovementType.Clamped;
		component4.scrollSensitivity = 42f;
		return obj.transform;
	}

	private static GameObject CreatePanel(Transform parent, string objectName, Vector2 size, Vector2 position)
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
			component2.color = new Color(0.08f, 0.08f, 0.1f, 0.96f);
			component2.raycastTarget = true;
		}
		return obj;
	}

	private static Transform CreateCarouselRoot(Transform parent, string objectName, Vector2 size, Vector2 position)
	{
		GameObject obj = new GameObject(objectName, typeof(RectTransform));
		obj.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = obj.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = size;
		return obj.transform;
	}

	private static GameObject CreateThinPanel(Transform parent, string objectName, Vector2 size, Vector2 position)
	{
		GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
		obj.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = obj.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = size;
		Image component2 = obj.GetComponent<Image>();
		component2.color = new Color(0.06f, 0.042f, 0.028f, 0.95f);
		component2.raycastTarget = true;
		Outline component3 = obj.GetComponent<Outline>();
		component3.effectColor = new Color(0.88f, 0.6f, 0.18f, 0.92f);
		component3.effectDistance = new Vector2(2.2f, -2.2f);
		component3.useGraphicAlpha = false;
		return obj;
	}

	private static GameObject CreateFullscreenDim(Transform parent, string objectName, Color color)
	{
		GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		obj.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = obj.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		Image component2 = obj.GetComponent<Image>();
		component2.color = color;
		component2.raycastTarget = true;
		return obj;
	}

	private static Button CreateButton(Transform parent, string objectName, string label, Vector2 position, Vector2 size, UnityAction action)
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
		CreateText(gameObject.transform, "Label", label, Vector2.zero, size, Mathf.Clamp(size.y * 0.42f, 19f, 58f), TextAlignmentOptions.Center).raycastTarget = false;
		return component2;
	}

	private static TMP_Text CreateText(Transform parent, string objectName, string value, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
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
		component2.fontSizeMin = Mathf.Max(11f, fontSize * 0.52f);
		component2.fontSizeMax = fontSize;
		component2.alignment = alignment;
		component2.color = Color.white;
		component2.textWrappingMode = TextWrappingModes.Normal;
		BattlePopupStyle.ApplyText(component2);
		return component2;
	}

	private static void ClearDynamicChildren(Transform parent)
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
		if (obj != null)
		{
			UnityEngine.Object.Destroy(obj);
		}
	}

	private static string T(string russian, string english, string turkish, string german = null)
	{
		return BattleLoreTutorialSession.T(russian, english, turkish, german);
	}
}
}
