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
public sealed class BattleLoreTutorialRuntime : MonoBehaviour
{
	private sealed class RewardChoiceData
	{
		public string Id;

		public BattleTileRarity Rarity;

		public string Name;

		public string Skill;

		public Sprite Sprite;
	}

	private const string BattleSceneName = "GameMahjongBattle";

	private const string HostObjectName = "BattleLoreTutorialRuntime";

	private const string RewardOverlayName = "BattleLoreTutorialRewardOverlay";

	private const string GuideOverlayName = "BattleLoreTutorialGuideOverlay";

	private const string HpBarsTextureResourcePath = "Mahjong/Sprites/BattleResult/HPBars";

	private const int OverlaySortingOrder = 30060;

	private const int GuideSortingOrder = 30058;

	private static readonly Rect PlayerHpBarSpriteRect = new Rect(332f, 32f, 240f, 960f);

	private static readonly Rect OpponentHpBarSpriteRect = new Rect(964f, 32f, 240f, 960f);

	private static Sprite cachedGuidePlayerHpBarSprite;

	private static Sprite cachedGuideOpponentHpBarSprite;

	private BattleMatchController controller;

	private BattleCombatSystem combatSystem;

	private BattleBoard playerBoard;

	private BattleBoard opponentBoard;

	private BattleBotController botController;

	private GameObject rewardOverlayRoot;

	private GameObject guideOverlayRoot;

	private GameObject guidePanelRoot;

	private TMP_Text guideTitleText;

	private TMP_Text guideBodyText;

	private Button guideButton;

	private Image guidePlayerHpFill;

	private Image guideOpponentHpFill;

	private TMP_Text guidePlayerHpText;

	private TMP_Text guideOpponentHpText;

	private Image guideArmorIcon;

	private Image guideCriticalIcon;

	private Image playerBoardDim;

	private Image opponentBoardDim;

	private TMP_Text boardHintText;

	private string selectedRareId = string.Empty;

	private string selectedEpicId = string.Empty;

	private readonly List<RewardChoiceData> rareRewardChoices = new List<RewardChoiceData>();

	private readonly List<RewardChoiceData> epicRewardChoices = new List<RewardChoiceData>();

	private bool firstHitGuideShown;

	private int playerHitCount;

	private bool tutorialPairDamageInProgress;

	private bool secondStagePlayerHitGuideShown;

	private bool tutorialGuidePauseActive;

	private bool secondStageAfterHitGuidePending;

	private int secondStagePlayerHitCount;

	private int thirdStageHitCount;

	private bool sixthStageFullscreenOpened;

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
		if (scene.IsValid() && string.Equals(scene.name, "GameMahjongBattle", StringComparison.Ordinal) && BattleLoreTutorialSession.IsActive)
		{
			SceneManager.MoveGameObjectToScene(new GameObject("BattleLoreTutorialRuntime", typeof(BattleLoreTutorialRuntime)), scene);
		}
	}

	private IEnumerator Start()
	{
		for (int i = 0; i < 60; i++)
		{
			if (!(controller == null))
			{
				break;
			}
			controller = UnityEngine.Object.FindAnyObjectByType<BattleMatchController>();
			if (controller != null)
			{
				break;
			}
			yield return null;
		}
		if (controller != null)
		{
			controller.MatchFinished += HandleMatchFinished;
		}
		ResolveBattleLinks();
		if (controller != null)
		{
			controller.SetTutorialDamageTextEmphasis(BattleLoreTutorialSession.ActiveStage <= 3);
		}
		if (BattleLoreTutorialSession.ActiveStage == 1)
		{
			SetupFirstStageGuide();
		}
		else if (BattleLoreTutorialSession.ActiveStage == 2)
		{
			SetupSecondStageGuide();
		}
		else if (BattleLoreTutorialSession.ActiveStage == 3)
		{
			SetupThirdStageGuide();
		}
		else if (BattleLoreTutorialSession.ActiveStage == 6)
		{
			SetupSixthStageFullscreenGuide();
		}
	}

	private void OnDestroy()
	{
		if (controller != null)
		{
			controller.MatchFinished -= HandleMatchFinished;
			controller.PlayerBoardFullscreenChanged -= HandleSixthStageFullscreenChanged;
		}
		if (controller != null)
		{
			controller.SetTutorialDamageTextEmphasis(enabled: false);
		}
		if (combatSystem != null)
		{
			combatSystem.DamageApplied -= HandleDamageApplied;
			combatSystem.DamageApplied -= HandleSecondStageDamageApplied;
			combatSystem.SetNetworkPairDamageSuppressed(value: false, BattleBoardSide.Player);
			combatSystem.SetNetworkPairDamageSuppressed(value: false);
		}
		if (playerBoard != null)
		{
			playerBoard.PairMatched -= HandleTutorialPlayerPairMatched;
			playerBoard.PairMatched -= HandleSecondStagePlayerPairMatched;
			playerBoard.PairMatched -= HandleThirdStagePlayerPairMatched;
		}
		SetPlayerBoardDim(visible: false);
	}

	private void ResolveBattleLinks()
	{
		combatSystem = UnityEngine.Object.FindAnyObjectByType<BattleCombatSystem>();
		botController = UnityEngine.Object.FindAnyObjectByType<BattleBotController>();
		BattleBoard[] array = UnityEngine.Object.FindObjectsByType<BattleBoard>(FindObjectsInactive.Include);
		foreach (BattleBoard battleBoard in array)
		{
			if (!(battleBoard == null))
			{
				if (battleBoard.Side == BattleBoardSide.Player)
				{
					playerBoard = battleBoard;
				}
				else if (battleBoard.Side == BattleBoardSide.Opponent)
				{
					opponentBoard = battleBoard;
				}
			}
		}
	}

	private void SetupFirstStageGuide()
	{
		if (botController != null)
		{
			botController.SetAutoStartOnEnable(value: false);
			botController.StopBot();
		}
		if (opponentBoard != null)
		{
			opponentBoard.SetInteractionLocked(value: true);
		}
		if (playerBoard != null)
		{
			playerBoard.SetInteractionLocked(value: true);
		}
		if (combatSystem != null)
		{
			combatSystem.SetNetworkPairDamageSuppressed(value: true, BattleBoardSide.Player);
			combatSystem.DamageApplied -= HandleDamageApplied;
			combatSystem.DamageApplied += HandleDamageApplied;
		}
		if (playerBoard != null)
		{
			playerBoard.PairMatched -= HandleTutorialPlayerPairMatched;
			playerBoard.PairMatched += HandleTutorialPlayerPairMatched;
		}
		EnsureOpponentBoardDim();
		EnsureBoardHint();
		ShowIntroGuide();
	}

	private void SetupSecondStageGuide()
	{
		if (botController != null)
		{
			botController.SetAutoStartOnEnable(value: false);
			botController.StopBot();
		}
		if (playerBoard != null)
		{
			playerBoard.SetInteractionLocked(value: true);
		}
		if (opponentBoard != null)
		{
			opponentBoard.SetInteractionLocked(value: true);
		}
		if (combatSystem != null)
		{
			combatSystem.SetNetworkPairDamageSuppressed(value: true, BattleBoardSide.Player);
			combatSystem.SetNetworkPairDamageSuppressed(value: true);
			combatSystem.DamageApplied -= HandleSecondStageDamageApplied;
			combatSystem.DamageApplied += HandleSecondStageDamageApplied;
		}
		secondStagePlayerHitCount = 0;
		if (playerBoard != null)
		{
			playerBoard.PairMatched -= HandleSecondStagePlayerPairMatched;
			playerBoard.PairMatched += HandleSecondStagePlayerPairMatched;
		}
		EnsureBoardHint();
		EnsurePlayerBoardDim();
		SetPlayerBoardDim(visible: false);
		ShowSecondStageIntroGuide();
	}

	private void SetupThirdStageGuide()
	{
		if (botController != null)
		{
			botController.SetAutoStartOnEnable(value: false);
			botController.StopBot();
		}
		if (playerBoard != null)
		{
			playerBoard.SetInteractionLocked(value: true);
		}
		if (opponentBoard != null)
		{
			opponentBoard.SetInteractionLocked(value: true);
		}
		if (combatSystem != null)
		{
			combatSystem.SetNetworkPairDamageSuppressed(value: true, BattleBoardSide.Player);
			combatSystem.SetNetworkPairDamageSuppressed(value: true);
		}
		if (playerBoard != null)
		{
			playerBoard.PairMatched -= HandleThirdStagePlayerPairMatched;
			playerBoard.PairMatched += HandleThirdStagePlayerPairMatched;
		}
		EnsureBoardHint();
		ShowThirdStageIntroGuide();
	}

	private void SetupSixthStageFullscreenGuide()
	{
		sixthStageFullscreenOpened = false;
		if (botController != null)
		{
			botController.SetAutoStartOnEnable(value: false);
			botController.StopBot();
		}
		if (playerBoard != null)
		{
			playerBoard.SetInteractionLocked(value: true);
		}
		if (opponentBoard != null)
		{
			opponentBoard.SetInteractionLocked(value: true);
		}
		EnsureBoardHint();
		if (controller != null)
		{
			if (controller.IsPlayerBoardFullscreen)
			{
				controller.ClosePlayerBoardFullscreenForTutorial();
			}
			controller.PlayerBoardFullscreenChanged -= HandleSixthStageFullscreenChanged;
			controller.PlayerBoardFullscreenChanged += HandleSixthStageFullscreenChanged;
		}
		ShowSixthStageFullscreenIntroGuide();
	}

	private void EnsurePlayerBoardDim()
	{
		if (!(playerBoardDim != null) && !(playerBoard == null) && !(playerBoard.BoardArea == null))
		{
			GameObject gameObject = new GameObject("TutorialPlayerBoardDim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			gameObject.transform.SetParent(playerBoard.BoardArea, worldPositionStays: false);
			RectTransform component = gameObject.GetComponent<RectTransform>();
			component.anchorMin = Vector2.zero;
			component.anchorMax = Vector2.one;
			component.offsetMin = Vector2.zero;
			component.offsetMax = Vector2.zero;
			playerBoardDim = gameObject.GetComponent<Image>();
			playerBoardDim.color = new Color(0f, 0f, 0f, 0.5f);
			playerBoardDim.raycastTarget = false;
			gameObject.SetActive(value: false);
			gameObject.transform.SetAsLastSibling();
		}
	}

	private void SetPlayerBoardDim(bool visible)
	{
		EnsurePlayerBoardDim();
		if (!(playerBoardDim == null))
		{
			playerBoardDim.gameObject.SetActive(visible);
			if (visible)
			{
				playerBoardDim.transform.SetAsLastSibling();
			}
		}
	}

	private void EnsureOpponentBoardDim()
	{
		if (!(opponentBoardDim != null) && !(opponentBoard == null) && !(opponentBoard.BoardArea == null))
		{
			GameObject gameObject = new GameObject("TutorialOpponentBoardDim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			gameObject.transform.SetParent(opponentBoard.BoardArea, worldPositionStays: false);
			RectTransform component = gameObject.GetComponent<RectTransform>();
			component.anchorMin = Vector2.zero;
			component.anchorMax = Vector2.one;
			component.offsetMin = Vector2.zero;
			component.offsetMax = Vector2.zero;
			opponentBoardDim = gameObject.GetComponent<Image>();
			opponentBoardDim.color = new Color(0f, 0f, 0f, 0.54f);
			opponentBoardDim.raycastTarget = false;
			gameObject.transform.SetAsLastSibling();
		}
	}

	private void ShowIntroGuide()
	{
		EnsureGuideOverlay();
		SetGuideText(T("Поле боя", "Battle Board", "Savaş Tahtaşı", "Kampffeld"), T("Слева находятся ваши камни. Справа находится доска противника. Сейчас противник не ходит: это первый тренировочный бой. Осмотрись, затем нажми «Понял».", "Your stones are on the left. The opponent board is on the right. The opponent is inactive in this first training fight. Look around, then press Got it.", "Solda senin taşların var. Sağda rakibin tahtası var. Bu ilk eğitim savaşında rakip aktif değil. Bak, sonra Anladım'a bas.", "Links sind deine Steine. Rechts ist das gegnerische Brett. Im ersten Training bleibt der Gegner inaktiv. Schau dich um und druecke Verstanden."), T("Понял", "Got it", "Anladim", "Verstanden"), delegate
		{
			if (playerBoard != null)
			{
				playerBoard.SetInteractionLocked(value: false);
			}
			HideGuidePanel();
			ShowBoardHint(T("Найдите два одинаковых открытых камня и нанесите первый урон противнику.", "Find two matching open stones and deal the first damage to the opponent.", "İki aynı açık taşı bul ve rakibe ilk hasarı ver.", "Finde zwei gleiche freie Steine und verursache den ersten Schaden."));
		});
	}

	private void ShowSecondStageIntroGuide()
	{
		EnsureGuideOverlay();
		SetGuideText(T("Первый удар врага", "Enemy Opens First", "Rakip Ilk Vurur", "Gegner beginnt"), T("В этом бою враг начнет первым. Посмотри, как HP уменьшается после входящего удара, затем ответь своими парами. Защиты зоной больше нет: важны темп, HP, armor и critical.", "In this fight the enemy opens first. Watch HP drop after the incoming hit, then answer with your own pairs. Zone guard is gone: tempo, HP, armor, and critical matter now.", "Bu savaşta rakip ilk vurur. Gelen darbeden sonra HP'nin nasıl azaldığını gör, sonra kendi eşlerinle karşılık ver. Bölge savunması yok: tempo, HP, armor ve critical önemli.", "In diesem Kampf beginnt der Gegner. Beobachte den HP-Verlust nach dem Treffer und antworte dann mit deinen Paaren. Zonenschutz gibt es nicht mehr: Tempo, HP, Armor und Critical zaehlen."), T("Принять удар", "Take Hit", "Vurusu Al", "Treffer nehmen"), delegate
		{
			HideGuidePanel();
			StartCoroutine(RunSecondStageOpeningAttack());
		});
	}

	private void ShowThirdStageIntroGuide()
	{
		EnsureGuideOverlay();
		SetGuideText(T("Armor и Critical", "Armor and Critical", "Armor ve Critical", "Armor und Critical"), T("Теперь смотрим не только на HP, но и на качество удара. Armor снижает полученный урон: поглощенная часть показывается рядом с числом урона в скобках. Critical это усиленный удар: он отмечается Crit! и наносит больше урона.", "Now watch not just HP, but the quality of a hit. Armor reduces received damage: the absorbed part appears next to the damage number in brackets. Critical is a stronger hit: it is marked Crit! and deals more damage.", "Şimdi sadece HP'ye değil, vuruşun kalitesine bak. Armor alınan hasarı azaltır: emilen kısım hasar sayısının yanında parantez içinde görünür. Critical daha güçlü vuruştur: Crit! yazısıyla işaretlenir ve daha çok hasar verir.", "Jetzt zaehlt nicht nur HP, sondern die Trefferqualitaet. Armor reduziert Schaden: Der absorbierte Teil steht neben der Schadenszahl in Klammern. Critical ist ein staerkerer Treffer: Er zeigt Crit! und verursacht mehr Schaden."), T("Понял", "Got it", "Anladim", "Verstanden"), delegate
		{
			if (playerBoard != null)
			{
				playerBoard.SetInteractionLocked(value: false);
			}
			HideGuidePanel();
			ShowBoardHint(T("Найдите первую пару: удар попадет в броню врага.", "Find the first pair: the hit will strike enemy armor.", "İlk eşi bul: vuruş rakibin armor'una çarpacak.", "Finde das erste Paar: Der Treffer geht in die Armor des Gegners."));
		});
		ShowGuideStatIcons(armor: true, critical: true);
	}

	private void HandleDamageApplied(BattleCombatSystem _, BattleBoardSide targetSide, int amount, int hp)
	{
		if (targetSide != BattleBoardSide.Opponent || amount <= 0)
		{
			return;
		}
		playerHitCount++;
		if (playerHitCount >= 2)
		{
			HideBoardHint();
			if (controller != null)
			{
				controller.ForceFinishMatch(playerWon: true);
			}
		}
		else if (!firstHitGuideShown)
		{
			firstHitGuideShown = true;
			if (playerBoard != null)
			{
				playerBoard.SetInteractionLocked(value: true);
			}
			ShowHpGuide();
		}
	}

	private void HandleTutorialPlayerPairMatched(BattleBoard _, BattleTile first, BattleTile second)
	{
		if (!tutorialPairDamageInProgress && !(combatSystem == null) && !combatSystem.IsCombatFinished)
		{
			StartCoroutine(ApplyTutorialPairDamageRoutine(first, second));
		}
	}

	private IEnumerator ApplyTutorialPairDamageRoutine(BattleTile first, BattleTile second)
	{
		tutorialPairDamageInProgress = true;
		if (playerBoard != null)
		{
			playerBoard.SetInteractionLocked(value: true);
		}
		if (controller != null)
		{
			yield return controller.PlayMatchedPairAttackSequence(BattleBoardSide.Player, first, second);
		}
		bool flag = false;
		if (combatSystem != null && combatSystem.IsCombatStarted && !combatSystem.IsCombatFinished)
		{
			int num = Mathf.Max(1, 2 - playerHitCount);
			int amount = Mathf.Max(1, Mathf.CeilToInt((float)combatSystem.OpponentHp / (float)num));
			flag = combatSystem.ApplyDamageToOpponent(amount);
		}
		if (!flag && playerBoard != null && !firstHitGuideShown)
		{
			playerBoard.SetInteractionLocked(value: false);
		}
		tutorialPairDamageInProgress = false;
	}

	private IEnumerator RunSecondStageOpeningAttack()
	{
		if (playerBoard != null)
		{
			playerBoard.SetInteractionLocked(value: true);
		}
		SetPlayerBoardDim(visible: true);
		ShowBoardHint(T("Враг атакует первым. Смотри на HP и готовь ответ.", "The enemy attacks first. Watch HP and prepare your answer.", "Rakip ilk saldırır. HP'yi izle ve cevabını hazırla.", "Der Gegner greift zuerst an. Achte auf HP und bereite die Antwort vor."));
		yield return new WaitForSeconds(0.8f);
		HideBoardHint();
		if (combatSystem != null && combatSystem.IsCombatStarted && !combatSystem.IsCombatFinished)
		{
			int damage = Mathf.Max(1, Mathf.CeilToInt((float)combatSystem.MaxPlayerHp * 0.22f));
			combatSystem.ApplyDamageToPlayer(new BattleDamageCalculator.DamageResult(damage, 0, crit: false));
		}
		yield return new WaitForSeconds(1.1f);
	}

	private void HandleSecondStageDamageApplied(BattleCombatSystem _, BattleBoardSide targetSide, int amount, int hp)
	{
		if (targetSide == BattleBoardSide.Player && amount > 0 && !secondStagePlayerHitGuideShown)
		{
			secondStagePlayerHitGuideShown = true;
			if (!secondStageAfterHitGuidePending)
			{
				StartCoroutine(ShowSecondStageAfterHitGuideDelayed());
			}
		}
	}

	private IEnumerator ShowSecondStageAfterHitGuideDelayed()
	{
		secondStageAfterHitGuidePending = true;
		yield return new WaitForSeconds(1.1f);
		ShowSecondStageAfterHitGuide();
		secondStageAfterHitGuidePending = false;
	}

	private void ShowSecondStageAfterHitGuide()
	{
		EnsureGuideOverlay();
		SetGuideText(T("Ответный темп", "Counter Tempo", "Karsi Tempo", "Gegentempo"), T("Первый урон прошел по вам. Теперь ход за вами: находите пары быстрее и давите врага уроном. Защита зоной убрана, поэтому исход боя решают HP, armor, critical и скорость сбора пар.", "The first hit landed on you. Now it is your turn: find pairs faster and pressure the enemy with damage. Zone guard is removed, so HP, armor, critical, and pair tempo decide the fight.", "Ilk hasar sana geldi. Simdi sira sende: esleri daha hizli bul ve hasarla rakibe baski kur. Bolge savunmasi kaldirildi; savasi HP, armor, critical ve es temposu belirler.", "Der erste Treffer ging auf dich. Jetzt bist du dran: Finde Paare schneller und setze den Gegner mit Schaden unter Druck. Zonenschutz ist entfernt; HP, Armor, Critical und Paar-Tempo entscheiden."), T("Добить врага", "Finish Enemy", "Rakibi Bitir", "Gegner beenden"), delegate
		{
			SetPlayerBoardDim(visible: false);
			if (playerBoard != null)
			{
				playerBoard.SetInteractionLocked(value: false);
			}
			HideGuidePanel();
			ShowBoardHint(T("Теперь атакуйте вы. Каждая найденная пара наносит урон без проверки защиты зоной.", "Now you attack. Every matched pair deals damage without a zone-guard check.", "Simdi sen saldir. Bulunan her es bolge savunmasi kontrolu olmadan hasar verir.", "Jetzt greifst du an. Jedes Paar verursacht Schaden ohne Zonenschutz-Pruefung."));
		});
	}

	private void HandleSecondStagePlayerPairMatched(BattleBoard _, BattleTile first, BattleTile second)
	{
		if (!tutorialPairDamageInProgress && !(combatSystem == null) && !combatSystem.IsCombatFinished)
		{
			StartCoroutine(ApplySecondStagePlayerDamageRoutine(first, second));
		}
	}

	private IEnumerator ApplySecondStagePlayerDamageRoutine(BattleTile first, BattleTile second)
	{
		tutorialPairDamageInProgress = true;
		if (playerBoard != null)
		{
			playerBoard.SetInteractionLocked(value: true);
		}
		HideBoardHint();
		if (controller != null)
		{
			yield return controller.PlayMatchedPairAttackSequence(BattleBoardSide.Player, first, second);
		}
		bool flag = false;
		if (combatSystem != null && combatSystem.IsCombatStarted && !combatSystem.IsCombatFinished)
		{
			secondStagePlayerHitCount++;
			int num = Mathf.Max(1, 4 - secondStagePlayerHitCount);
			bool num2 = secondStagePlayerHitCount >= 3;
			int amount = (num2 ? Mathf.Max(1, combatSystem.OpponentHp) : Mathf.Max(1, Mathf.CeilToInt((float)combatSystem.OpponentHp / (float)num)));
			flag = combatSystem.ApplyDamageToOpponent(amount);
			if (num2 && !combatSystem.IsCombatFinished && controller != null)
			{
				controller.ForceFinishMatch(playerWon: true);
			}
		}
		if (playerBoard != null && flag && combatSystem != null && !combatSystem.IsCombatFinished)
		{
			playerBoard.SetInteractionLocked(value: false);
			ShowBoardHint(T("Продолжайте атаковать: каждая следующая пара приближает победу.", "Keep attacking: every next pair brings victory closer.", "Saldirmaya devam et: her sonraki es zaferi yaklastirir.", "Greif weiter an: Jedes weitere Paar bringt den Sieg naeher."));
		}
		tutorialPairDamageInProgress = false;
	}

	private void HandleThirdStagePlayerPairMatched(BattleBoard _, BattleTile first, BattleTile second)
	{
		if (!tutorialPairDamageInProgress && !(combatSystem == null) && !combatSystem.IsCombatFinished)
		{
			StartCoroutine(ApplyThirdStagePlayerDamageRoutine(first, second));
		}
	}

	private IEnumerator ApplyThirdStagePlayerDamageRoutine(BattleTile first, BattleTile second)
	{
		tutorialPairDamageInProgress = true;
		thirdStageHitCount++;
		if (playerBoard != null)
		{
			playerBoard.SetInteractionLocked(value: true);
		}
		HideBoardHint();
		if (controller != null)
		{
			yield return controller.PlayMatchedPairAttackSequence(BattleBoardSide.Player, first, second);
		}
		bool damageApplied = false;
		if (combatSystem != null && combatSystem.IsCombatStarted && !combatSystem.IsCombatFinished)
		{
			bool flag = ((playerBoard != null && playerBoard.ActiveTileCount != 0) ? 1 : 0) <= (false ? 1 : 0);
			BattleDamageCalculator.DamageResult result = BuildThirdStageDamageResult(flag);
			damageApplied = combatSystem.ApplyDamageToOpponent(result);
			if (flag && !combatSystem.IsCombatFinished && controller != null)
			{
				controller.ForceFinishMatch(playerWon: true);
			}
		}
		yield return new WaitForSeconds(1f);
		if (thirdStageHitCount == 1)
		{
			ShowThirdStageArmorGuide();
		}
		else if (thirdStageHitCount == 2)
		{
			ShowThirdStageCriticalGuide();
		}
		else if (playerBoard != null && damageApplied && combatSystem != null && !combatSystem.IsCombatFinished)
		{
			playerBoard.SetInteractionLocked(value: false);
			ShowBoardHint(T("Оставшиеся пары добьют врага. Сравни обычный урон, броню и critical.", "The remaining pairs will finish the enemy. Compare normal damage, armor, and critical.", "Kalan esler rakibi bitirecek. Normal hasar, armor ve critical farkini karsilastir.", "Die restlichen Paare beenden den Gegner. Vergleiche normalen Schaden, Armor und Critical."));
		}
		tutorialPairDamageInProgress = false;
	}

	private BattleDamageCalculator.DamageResult BuildThirdStageDamageResult(bool lastPair)
	{
		if (combatSystem == null)
		{
			return new BattleDamageCalculator.DamageResult(1, crit: false);
		}
		if (lastPair)
		{
			return new BattleDamageCalculator.DamageResult(Mathf.Max(1, combatSystem.OpponentHp), 0, crit: true);
		}
		if (thirdStageHitCount <= 1)
		{
			int num = Mathf.Max(1, Mathf.CeilToInt((float)combatSystem.MaxOpponentHp * 0.26f));
			int num2 = Mathf.Max(1, Mathf.CeilToInt((float)num * 0.38f));
			return new BattleDamageCalculator.DamageResult(Mathf.Max(1, num - num2), num2, crit: false);
		}
		if (thirdStageHitCount == 2)
		{
			return new BattleDamageCalculator.DamageResult(Mathf.Max(1, Mathf.CeilToInt((float)combatSystem.MaxOpponentHp * 0.36f)), 0, crit: true);
		}
		int num3 = ((playerBoard != null) ? playerBoard.ActiveTileCount : 0);
		int num4 = Mathf.Max(1, Mathf.CeilToInt((float)num3 / 2f) + 1);
		return new BattleDamageCalculator.DamageResult(Mathf.Max(1, Mathf.CeilToInt((float)combatSystem.OpponentHp / (float)num4)), 0, crit: true);
	}

	private void ShowThirdStageArmorGuide()
	{
		EnsureGuideOverlay();
		SetGuideText(T("Armor поглощает часть удара", "Armor Absorbs Part of a Hit", "Armor Vurusun Bir Kismini Emer", "Armor absorbiert Schaden"), T("Пример чтения урона: иконка удара + число это HP, снятое с врага. Иконка щита + число это урон, который забрала броня. Чем выше armor, тем меньше обычный и critical-урон проходит в HP.", "Damage example: strike icon + number is HP removed from the enemy. Shield icon + number is damage absorbed by armor. Higher armor means less normal and critical damage reaches HP.", "Hasar örneği: vuruş ikonu + sayı rakipten giden HP'dir. Kalkan ikonu + sayı armor'un emdiği hasardır. Armor ne kadar yüksekse normal ve critical hasar HP'ye o kadar az geçer.", "Schadensbeispiel: Treffer-Icon + Zahl ist HP-Schaden. Schild-Icon + Zahl ist von Armor absorbierter Schaden. Mehr Armor bedeutet weniger normalen und kritischen Schaden auf HP."), T("Дальше", "Continue", "Devam", "Weiter"), delegate
		{
			if (playerBoard != null)
			{
				playerBoard.SetInteractionLocked(value: false);
			}
			HideGuidePanel();
			ShowBoardHint(T("Найдите следующую пару: теперь покажем critical-удар.", "Find the next pair: now we show a critical hit.", "Sonraki eşi bul: şimdi critical vuruş gösterilecek.", "Finde das naechste Paar: Jetzt zeigen wir Critical."));
		});
		ShowGuideStatIcons(armor: true, critical: false);
	}

	private void ShowThirdStageCriticalGuide()
	{
		EnsureGuideOverlay();
		SetGuideText(T("Critical усиливает удар", "Critical Amplifies a Hit", "Critical Vurusu Guclendirir", "Critical verstaerkt Treffer"), T("Пример чтения урона: меч с молнией + число это critical-урон. Он сильнее обычного удара и зависит от шанса крита и множителя critical damage.", "Damage example: sword with lightning + number is critical damage. It hits harder than a normal strike and depends on crit chance and critical damage multiplier.", "Hasar örneği: şimşekli kılıç + sayı critical hasardır. Normal vuruştan daha sert vurur ve crit şansı ile critical damage çarpanına bağlıdır.", "Schadensbeispiel: Schwert mit Blitz + Zahl ist Critical-Schaden. Er ist staerker als normaler Schaden und haengt von Crit-Chance und Critical-Damage ab."), T("Добить", "Finish", "Bitir", "Beenden"), delegate
		{
			if (playerBoard != null)
			{
				playerBoard.SetInteractionLocked(value: false);
			}
			HideGuidePanel();
			ShowBoardHint(T("Добейте врага оставшимися парами.", "Finish the enemy with the remaining pairs.", "Kalan eslerle rakibi bitir.", "Beende den Gegner mit den restlichen Paaren."));
		});
		ShowGuideStatIcons(armor: false, critical: true);
	}

	private void ShowSixthStageFullscreenIntroGuide()
	{
		EnsureGuideOverlay();
		SetGuideText(T("Открой FULL", "Open FULL", "FULL'u Aç", "FULL oeffnen"), T("Перед финальным боем нужно научиться расширять свою доску. Нажми кнопку FULL в правом верхнем углу поля: бой раскроется на весь экран, а лишние панели уйдут.", "Before the final fight, learn to expand your board. Press the FULL button in the top-right of the board: the fight opens fullscreen and extra panels move away.", "Final savaştan önce tahtanı büyütmeyi öğren. Tahtanın sağ üstündeki FULL düğmesine bas: savaş tam ekrana açılır ve fazla paneller çekilir.", "Vor dem Finale lernst du, dein Brett zu vergroessern. Druecke FULL oben rechts am Brett: Der Kampf oeffnet sich im Vollbild und die Nebenfelder verschwinden."), T("Понял", "Got it", "Anladim", "Verstanden"), delegate
		{
			tutorialGuidePauseActive = false;
			HideGuidePanel();
			ShowBoardHint(T("Нажми настоящую кнопку FULL справа сверху на доске.", "Press the real FULL button at the top-right of the board.", "Tahtanın sağ üstündeki gerçek FULL düğmesine bas.", "Druecke die echte FULL-Taste oben rechts am Brett."));
		});
	}

	private void HandleSixthStageFullscreenChanged(BattleMatchController match, bool fullscreen)
	{
		if (BattleLoreTutorialSession.ActiveStage == 6 && fullscreen && !sixthStageFullscreenOpened)
		{
			sixthStageFullscreenOpened = true;
			ShowSixthStageFullscreenOpenedGuide();
		}
	}

	private void ShowSixthStageFullscreenOpenedGuide()
	{
		EnsureGuideOverlay();
		SetGuideText(T("Теперь видно поле", "Now the Board Is Clear", "Tahta Artik Net", "Jetzt ist das Brett klar"), T("Это режим FULL. Пользуйся им, когда тайлы маленькие или нужно увидеть свободные пары без шума лобби. Кнопка BACK вернет обычный вид. Сейчас найди пары и заверши финальный бой.", "This is FULL mode. Use it when tiles feel small or you need to read free pairs without lobby noise. BACK returns the normal view. Now find pairs and finish the final fight.", "Bu FULL modu. Taşlar küçük gelirse veya serbest eşleri daha rahat görmek istersen kullan. BACK normal görünüme döndürür. Şimdi eşleri bul ve final savaşı bitir.", "Das ist der FULL-Modus. Nutze ihn, wenn Steine klein wirken oder du freie Paare ohne UI-Rauschen lesen willst. BACK kehrt zurueck. Finde jetzt Paare und beende das Finale."), T("Играть", "Play", "Oyna", "Spielen"), delegate
		{
			if (playerBoard != null)
			{
				playerBoard.SetInteractionLocked(value: false);
			}
			tutorialGuidePauseActive = false;
			HideGuidePanel();
			ShowBoardHint(T("Играй на большой доске. После победы обучение завершится.", "Play on the larger board. Training ends after victory.", "Büyük tahtada oyna. Zaferden sonra eğitim biter.", "Spiele auf dem grossen Brett. Nach dem Sieg endet das Training."));
		});
	}

	private void ShowHpGuide()
	{
		EnsureGuideOverlay();
		string text = ((combatSystem != null) ? $"{combatSystem.PlayerHp}/{combatSystem.MaxPlayerHp}" : "HP");
		string text2 = ((combatSystem != null) ? $"{combatSystem.OpponentHp}/{combatSystem.MaxOpponentHp}" : "HP");
		SetGuideText(T("Здоровье", "Health Bars", "Can Barlari", "Lebenspunkte"), T("Это HP-бары боя.\n\nВаше здоровье: " + text + "\nЗдоровье противника: " + text2 + "\n\nПо ним видно, кто ближе к победе. Каждая найденная пара превращается в удар.", "These are the battle HP bars.\n\nYour health: " + text + "\nOpponent health: " + text2 + "\n\nThey show who is closer to victory. Every matched pair becomes an attack.", "Bunlar savaş HP barları.\n\nSenin canın: " + text + "\nRakip canı: " + text2 + "\n\nZafere kimin yakın olduğunu gösterir. Her eşleşme saldırıya dönüşür.", "Das sind die HP-Leisten.\n\nDeine HP: " + text + "\nGegner-HP: " + text2 + "\n\nSie zeigen, wer dem Sieg naeher ist. Jedes Paar wird zum Angriff."), T("Понял", "Got it", "Anladim", "Verstanden"), delegate
		{
			if (playerBoard != null)
			{
				playerBoard.SetInteractionLocked(value: false);
			}
			HideGuidePanel();
			ShowBoardHint(T("Отлично. Найдите еще одну пару: после второго удара обучение завершится победой.", "Good. Find one more pair: after the second hit this training fight ends in victory.", "Güzel. Bir eş daha bul: ikinci vuruşla eğitim savaşı zaferle biter.", "Gut. Finde noch ein Paar: Nach dem zweiten Treffer endet das Training mit einem Sieg."));
		});
		EnsureGuideHpBars();
		RefreshGuideHpBars();
	}

	private void EnsureGuideOverlay()
	{
		if (!(guideOverlayRoot != null))
		{
			Canvas canvas = CreateOverlayCanvas("BattleLoreTutorialGuideCanvas", 30058);
			guideOverlayRoot = new GameObject("BattleLoreTutorialGuideOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			guideOverlayRoot.transform.SetParent(canvas.transform, worldPositionStays: false);
			RectTransform component = guideOverlayRoot.GetComponent<RectTransform>();
			component.anchorMin = Vector2.zero;
			component.anchorMax = Vector2.one;
			component.offsetMin = Vector2.zero;
			component.offsetMax = Vector2.zero;
			Image component2 = guideOverlayRoot.GetComponent<Image>();
			component2.color = new Color(0f, 0f, 0f, 0.18f);
			component2.raycastTarget = false;
			guidePanelRoot = CreatePanel(guideOverlayRoot.transform, "GuidePanel", new Vector2(2180f, 980f), new Vector2(0f, 0f));
			guideTitleText = CreateText(guidePanelRoot.transform, "GuideTitle", string.Empty, new Vector2(0f, 310f), new Vector2(1760f, 110f), 76f, TextAlignmentOptions.Center);
			guideBodyText = CreateText(guidePanelRoot.transform, "GuideBody", string.Empty, new Vector2(0f, 82f), new Vector2(1780f, 300f), 46f, TextAlignmentOptions.Center);
			guideButton = CreateButton(guidePanelRoot.transform, "ButtonGuideUnderstood", T("Понял", "Got it", "Anladim", "Verstanden"), new Vector2(0f, -316f), new Vector2(680f, 136f), null);
		}
	}

	private void SetGuideText(string title, string body, string buttonLabel, UnityAction action)
	{
		SetTutorialGuidePause(paused: true);
		if (guideOverlayRoot != null)
		{
			guideOverlayRoot.SetActive(value: true);
		}
		if (guidePanelRoot != null)
		{
			guidePanelRoot.SetActive(value: true);
		}
		HideGuideStatIcons();
		if (guideTitleText != null)
		{
			guideTitleText.text = title;
		}
		if (guideBodyText != null)
		{
			guideBodyText.text = body;
		}
		if (guideButton != null)
		{
			guideButton.onClick.RemoveAllListeners();
			if (action != null)
			{
				guideButton.onClick.AddListener(action);
			}
			TMP_Text componentInChildren = guideButton.GetComponentInChildren<TMP_Text>(includeInactive: true);
			if (componentInChildren != null)
			{
				componentInChildren.text = buttonLabel;
			}
		}
	}

	private void HideGuidePanel()
	{
		if (guidePanelRoot != null)
		{
			guidePanelRoot.SetActive(value: false);
		}
	}

	private void ShowGuideStatIcons(bool armor, bool critical)
	{
		if (guidePanelRoot == null)
		{
			return;
		}
		if (armor)
		{
			guideArmorIcon = BattleStatIconProvider.EnsureIcon(guidePanelRoot.transform, "GuideArmorIcon", BattleStatIconKind.Armor, critical ? new Vector2(-132f, 206f) : new Vector2(0f, 206f), new Vector2(132f, 132f));
			if (guideArmorIcon != null)
			{
				guideArmorIcon.gameObject.SetActive(value: true);
			}
		}
		if (critical)
		{
			guideCriticalIcon = BattleStatIconProvider.EnsureIcon(guidePanelRoot.transform, "GuideCriticalIcon", BattleStatIconKind.CriticalDamage, armor ? new Vector2(132f, 206f) : new Vector2(0f, 206f), new Vector2(132f, 132f));
			if (guideCriticalIcon != null)
			{
				guideCriticalIcon.gameObject.SetActive(value: true);
			}
		}
	}

	private void HideGuideStatIcons()
	{
		if (guideArmorIcon != null)
		{
			guideArmorIcon.gameObject.SetActive(value: false);
		}
		if (guideCriticalIcon != null)
		{
			guideCriticalIcon.gameObject.SetActive(value: false);
		}
	}

	private void SetTutorialGuidePause(bool paused)
	{
		tutorialGuidePauseActive = paused;
		if (paused)
		{
			if (playerBoard != null)
			{
				playerBoard.SetInteractionLocked(value: true);
			}
			if (opponentBoard != null)
			{
				opponentBoard.SetInteractionLocked(value: true);
			}
			if (botController != null)
			{
				botController.StopBot();
			}
		}
	}

	private void EnsureGuideHpBars()
	{
		if (!(guidePanelRoot == null) && !(guidePlayerHpFill != null))
		{
			CreateText(guidePanelRoot.transform, "GuidePlayerHpLabel", T("Ваше HP", "Your HP", "Senin HP", "Deine HP"), new Vector2(-520f, -132f), new Vector2(420f, 48f), 32f, TextAlignmentOptions.Center);
			CreateText(guidePanelRoot.transform, "GuideOpponentHpLabel", T("HP противника", "Opponent HP", "Rakip HP", "Gegner-HP"), new Vector2(520f, -132f), new Vector2(420f, 48f), 32f, TextAlignmentOptions.Center);
			guidePlayerHpFill = CreateGuideHpBar(guidePanelRoot.transform, "GuidePlayerHpBar", new Vector2(-520f, -198f), playerSide: true, LoadGuideHpBarSprite(playerSide: true), new Color(0.25f, 0.9f, 0.35f, 1f), out guidePlayerHpText);
			guideOpponentHpFill = CreateGuideHpBar(guidePanelRoot.transform, "GuideOpponentHpBar", new Vector2(520f, -198f), playerSide: false, LoadGuideHpBarSprite(playerSide: false), new Color(1f, 0.28f, 0.22f, 1f), out guideOpponentHpText);
		}
	}

	private void RefreshGuideHpBars()
	{
		if (!(combatSystem == null))
		{
			SetGuideHpBar(guidePlayerHpFill, guidePlayerHpText, combatSystem.PlayerHp, combatSystem.MaxPlayerHp);
			SetGuideHpBar(guideOpponentHpFill, guideOpponentHpText, combatSystem.OpponentHp, combatSystem.MaxOpponentHp);
		}
	}

	private static Image CreateGuideHpBar(Transform parent, string objectName, Vector2 position, bool playerSide, Sprite barSprite, Color fillColor, out TMP_Text valueText)
	{
		GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = new Vector2(620f, 70f);
		GameObject gameObject2 = new GameObject("BarSprite", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
		RectTransform component2 = gameObject2.GetComponent<RectTransform>();
		component2.anchorMin = new Vector2(0.5f, 0.5f);
		component2.anchorMax = new Vector2(0.5f, 0.5f);
		component2.pivot = new Vector2(0.5f, 0.5f);
		component2.anchoredPosition = Vector2.zero;
		component2.sizeDelta = ((barSprite != null) ? new Vector2(70f, 620f) : new Vector2(620f, 70f));
		if (barSprite != null)
		{
			component2.localRotation = Quaternion.Euler(0f, 0f, -90f);
			component2.localScale = new Vector3(1f, playerSide ? 1f : (-1f), 1f);
		}
		Image component3 = gameObject2.GetComponent<Image>();
		component3.sprite = barSprite;
		component3.type = Image.Type.Simple;
		component3.preserveAspect = false;
		component3.color = ((barSprite != null) ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.62f));
		component3.raycastTarget = false;
		GameObject obj = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		obj.transform.SetParent(gameObject2.transform, worldPositionStays: false);
		RectTransform component4 = obj.GetComponent<RectTransform>();
		component4.anchorMin = new Vector2(0f, 0f);
		component4.anchorMax = new Vector2(1f, 1f);
		component4.offsetMin = Vector2.zero;
		component4.offsetMax = Vector2.zero;
		Image component5 = obj.GetComponent<Image>();
		component5.sprite = barSprite;
		component5.color = ((barSprite != null) ? Color.white : fillColor);
		component5.type = ((barSprite != null) ? Image.Type.Filled : Image.Type.Simple);
		component5.fillMethod = Image.FillMethod.Vertical;
		component5.fillOrigin = 0;
		component5.preserveAspect = false;
		component5.raycastTarget = false;
		valueText = CreateText(gameObject.transform, "Value", string.Empty, Vector2.zero, new Vector2(500f, 42f), 26f, TextAlignmentOptions.Center);
		valueText.raycastTarget = false;
		return component5;
	}

	private static Sprite LoadGuideHpBarSprite(bool playerSide)
	{
		if (playerSide && cachedGuidePlayerHpBarSprite != null)
		{
			return cachedGuidePlayerHpBarSprite;
		}
		if (!playerSide && cachedGuideOpponentHpBarSprite != null)
		{
			return cachedGuideOpponentHpBarSprite;
		}
		Texture2D texture2D = Resources.Load<Texture2D>("Mahjong/Sprites/BattleResult/HPBars");
		if (texture2D == null)
		{
			Sprite sprite = Resources.Load<Sprite>("Mahjong/Sprites/BattleResult/HPBars");
			texture2D = ((sprite != null) ? sprite.texture : null);
		}
		if (texture2D == null)
		{
			return null;
		}
		Rect rect = ClampRectToTexture(playerSide ? PlayerHpBarSpriteRect : OpponentHpBarSpriteRect, texture2D);
		Sprite sprite2 = Sprite.Create(texture2D, rect, new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
		sprite2.name = (playerSide ? "GuidePlayerHpBar" : "GuideOpponentHpBar");
		if (playerSide)
		{
			cachedGuidePlayerHpBarSprite = sprite2;
		}
		else
		{
			cachedGuideOpponentHpBarSprite = sprite2;
		}
		return sprite2;
	}

	private static Rect ClampRectToTexture(Rect rect, Texture2D texture)
	{
		if (texture == null)
		{
			return rect;
		}
		float num = Mathf.Clamp(rect.x, 0f, Mathf.Max(0f, (float)texture.width - 1f));
		float num2 = Mathf.Clamp(rect.y, 0f, Mathf.Max(0f, (float)texture.height - 1f));
		float width = Mathf.Clamp(rect.width, 1f, (float)texture.width - num);
		float height = Mathf.Clamp(rect.height, 1f, (float)texture.height - num2);
		return new Rect(num, num2, width, height);
	}

	private static void SetGuideHpBar(Image fill, TMP_Text text, int hp, int maxHp)
	{
		if (fill != null)
		{
			float num = ((maxHp > 0) ? Mathf.Clamp01((float)Mathf.Max(0, hp) / (float)maxHp) : 0f);
			if (fill.type == Image.Type.Filled)
			{
				fill.fillAmount = num;
			}
			else
			{
				fill.rectTransform.anchorMax = new Vector2(num, 1f);
			}
		}
		if (text != null)
		{
			text.text = ((maxHp > 0) ? $"{Mathf.Max(0, hp)}/{maxHp}" : "HP");
		}
	}

	private void EnsureBoardHint()
	{
		if (!(boardHintText != null) && !(playerBoard == null) && !(playerBoard.BoardArea == null))
		{
			boardHintText = CreateText(playerBoard.BoardArea, "TutorialBoardHint", string.Empty, new Vector2(0f, 230f), new Vector2(820f, 150f), 46f, TextAlignmentOptions.Center);
			boardHintText.color = new Color(1f, 0.96f, 0.68f, 1f);
			boardHintText.outlineWidth = 0.22f;
			boardHintText.outlineColor = Color.black;
			boardHintText.raycastTarget = false;
			boardHintText.gameObject.SetActive(value: false);
			boardHintText.transform.SetAsLastSibling();
		}
	}

	private void ShowBoardHint(string text)
	{
		EnsureBoardHint();
		if (!(boardHintText == null))
		{
			boardHintText.text = text;
			boardHintText.gameObject.SetActive(value: true);
			boardHintText.transform.SetAsLastSibling();
		}
	}

	private void HideBoardHint()
	{
		if (boardHintText != null)
		{
			boardHintText.gameObject.SetActive(value: false);
		}
	}

	private void HandleMatchFinished(BattleMatchController match, bool playerWon)
	{
		if (!BattleLoreTutorialSession.IsActive)
		{
			return;
		}
		if (playerBoard != null)
		{
			playerBoard.PairMatched -= HandleTutorialPlayerPairMatched;
		}
		if (combatSystem != null)
		{
			combatSystem.DamageApplied -= HandleSecondStageDamageApplied;
			combatSystem.SetNetworkPairDamageSuppressed(value: false, BattleBoardSide.Player);
			combatSystem.SetNetworkPairDamageSuppressed(value: false);
		}
		if (playerBoard != null)
		{
			playerBoard.PairMatched -= HandleSecondStagePlayerPairMatched;
		}
		if (playerBoard != null)
		{
			playerBoard.PairMatched -= HandleThirdStagePlayerPairMatched;
		}
		if (controller != null)
		{
			controller.PlayerBoardFullscreenChanged -= HandleSixthStageFullscreenChanged;
		}
		SetPlayerBoardDim(visible: false);
		bool flag = false;
		if (playerWon)
		{
			int activeStage = BattleLoreTutorialSession.ActiveStage;
			BattleLoreTutorialSession.GrantStageReward(BattleLoreTutorialSession.ActiveStage);
			BattleLoreTutorialSession.CompleteActiveStage();
			flag = activeStage >= 6 && !BattleLoreTutorialSession.RewardClaimed;
			if (!flag)
			{
				BattleLoreTutorialSession.RequestOpenOnLobbyReturn();
			}
		}
		BattleLoreTutorialSession.ClearActive();
		if (flag)
		{
			StartCoroutine(ShowRewardAfterResultDelay());
		}
	}

	private IEnumerator ShowRewardAfterResultDelay()
	{
		yield return new WaitForSecondsRealtime(1.2f);
		EnsureRewardOverlay();
		RefreshRewardOverlay();
		rewardOverlayRoot.SetActive(value: true);
		rewardOverlayRoot.transform.SetAsLastSibling();
	}

	private void EnsureRewardOverlay()
	{
		if (!(rewardOverlayRoot != null))
		{
			Canvas canvas = CreateRewardCanvas();
			rewardOverlayRoot = new GameObject("BattleLoreTutorialRewardOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			rewardOverlayRoot.transform.SetParent(canvas.transform, worldPositionStays: false);
			RectTransform component = rewardOverlayRoot.GetComponent<RectTransform>();
			component.anchorMin = Vector2.zero;
			component.anchorMax = Vector2.one;
			component.offsetMin = Vector2.zero;
			component.offsetMax = Vector2.zero;
			rewardOverlayRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
			GameObject obj = CreatePanel(rewardOverlayRoot.transform, "RewardPanel", new Vector2(1980f, 920f), Vector2.zero);
			CreateText(obj.transform, "Title", T("Часть силы остается с тобой", "A Piece of Power Stays With You", "Gucun Bir Parcasi Sende Kalir", "Ein Teil der Kraft bleibt"), new Vector2(0f, 360f), new Vector2(1500f, 70f), 52f, TextAlignmentOptions.Center);
			CreateText(obj.transform, "Hint", T("Ты получаешь один редкий и один эпический камень. Это старт твоего настоящего билда.", "You receive one rare and one epic stone. This starts your real build.", "Bir rare ve bir epic taş alıyorsun. Gercek build burada baslar.", "Du erhaeltst einen rare und einen epic Stein. Hier beginnt dein echter Build."), new Vector2(0f, 302f), new Vector2(1700f, 58f), 28f, TextAlignmentOptions.Center);
			CreateText(obj.transform, "RareTitle", "RARE", new Vector2(-500f, 224f), new Vector2(680f, 52f), 38f, TextAlignmentOptions.Center);
			CreateText(obj.transform, "EpicTitle", "EPIC", new Vector2(500f, 224f), new Vector2(680f, 52f), 38f, TextAlignmentOptions.Center);
			CreateButton(obj.transform, "ButtonClaimReward", T("Забрать", "Claim", "Al", "Nehmen"), new Vector2(0f, -368f), new Vector2(520f, 92f), ClaimReward);
			rewardOverlayRoot.SetActive(value: false);
		}
	}

	private void RefreshRewardOverlay()
	{
		BattleTileStore store = ((BattleTileStore.I != null) ? BattleTileStore.I : UnityEngine.Object.FindAnyObjectByType<BattleTileStore>());
		Transform transform = ((rewardOverlayRoot != null) ? rewardOverlayRoot.transform.Find("RewardPanel") : null);
		if (!(transform == null))
		{
			ClearNamedChildren(transform, "RareChoice_", "EpicChoice_");
			EnsureRewardChoicesCached(store);
			if (string.IsNullOrWhiteSpace(selectedRareId) && rareRewardChoices.Count > 0)
			{
				selectedRareId = rareRewardChoices[0].Id;
			}
			if (string.IsNullOrWhiteSpace(selectedEpicId) && epicRewardChoices.Count > 0)
			{
				selectedEpicId = epicRewardChoices[0].Id;
			}
			for (int i = 0; i < rareRewardChoices.Count; i++)
			{
				CreateRewardChoice(transform, "RareChoice_" + i, rareRewardChoices[i], new Vector2(-500f, 22f), selectedRareId == rareRewardChoices[i].Id);
			}
			for (int j = 0; j < epicRewardChoices.Count; j++)
			{
				CreateRewardChoice(transform, "EpicChoice_" + j, epicRewardChoices[j], new Vector2(500f, 22f), selectedEpicId == epicRewardChoices[j].Id);
			}
		}
	}

	private void EnsureRewardChoicesCached(BattleTileStore store)
	{
		if (rareRewardChoices.Count == 0)
		{
			CacheRewardChoices(rareRewardChoices, BattleLoreTutorialSession.GetRewardCandidates(store, BattleTileRarity.Rare, 1));
		}
		if (epicRewardChoices.Count == 0)
		{
			CacheRewardChoices(epicRewardChoices, BattleLoreTutorialSession.GetRewardCandidates(store, BattleTileRarity.Epic, 1));
		}
	}

	private static void CacheRewardChoices(List<RewardChoiceData> target, IReadOnlyList<BattleTileData> source)
	{
		target.Clear();
		if (source == null)
		{
			return;
		}
		for (int i = 0; i < source.Count; i++)
		{
			BattleTileData battleTileData = source[i];
			if (battleTileData != null && !string.IsNullOrWhiteSpace(battleTileData.Id))
			{
				target.Add(new RewardChoiceData
				{
					Id = battleTileData.Id,
					Rarity = battleTileData.Rarity,
					Name = ResolveTileName(battleTileData),
					Skill = ResolveTileSkill(battleTileData),
					Sprite = ResolveTileSprite(battleTileData)
				});
			}
		}
	}

	private void CreateRewardChoice(Transform parent, string objectName, RewardChoiceData data, Vector2 position, bool selected)
	{
		Button button = CreateButton(parent, objectName, string.Empty, position, new Vector2(250f, 460f), delegate
		{
			if (data.Rarity == BattleTileRarity.Rare)
			{
				selectedRareId = data.Id;
			}
			else if (data.Rarity == BattleTileRarity.Epic)
			{
				selectedEpicId = data.Id;
			}
			RefreshRewardOverlay();
		});
		button.GetComponent<Image>().color = (selected ? new Color(0.28f, 0.2f, 0.06f, 0.96f) : new Color(0.07f, 0.07f, 0.09f, 0.88f));
		Image image = CreateImage(button.transform, "Face", new Vector2(0f, 96f), new Vector2(160f, 190f));
		image.sprite = data.Sprite;
		image.enabled = image.sprite != null;
		image.preserveAspect = true;
		CreateText(button.transform, "Name", data.Name, new Vector2(0f, -68f), new Vector2(218f, 88f), 25f, TextAlignmentOptions.Center);
		CreateText(button.transform, "Skill", data.Skill, new Vector2(0f, -166f), new Vector2(218f, 102f), 20f, TextAlignmentOptions.Center);
	}

	private void ClaimReward()
	{
		PlayerProfile playerProfile = ((ProfileService.I != null) ? ProfileService.I.Current : null);
		BattleTileStore battleTileStore = ((BattleTileStore.I != null) ? BattleTileStore.I : UnityEngine.Object.FindAnyObjectByType<BattleTileStore>());
		if (playerProfile != null && !(battleTileStore == null) && !string.IsNullOrWhiteSpace(selectedRareId) && !string.IsNullOrWhiteSpace(selectedEpicId))
		{
			BattleTileInventoryService.GrantTileCopy(playerProfile, battleTileStore, selectedRareId, out var isNew);
			BattleTileInventoryService.GrantTileCopy(playerProfile, battleTileStore, selectedEpicId, out isNew);
			BattleLoreTutorialSession.MarkRewardClaimed();
			BattleLoreTutorialSession.ClearActive();
			if (ProfileService.I != null)
			{
				ProfileService.I.Save();
				ProfileService.I.NotifyProfileChanged();
			}
			if (rewardOverlayRoot != null)
			{
				rewardOverlayRoot.SetActive(value: false);
			}
		}
	}

	private static Canvas CreateRewardCanvas()
	{
		return CreateOverlayCanvas("BattleLoreTutorialRewardCanvas", 30060);
	}

	private static Canvas CreateOverlayCanvas(string objectName, int sortingOrder)
	{
		GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		Canvas component = obj.GetComponent<Canvas>();
		component.renderMode = RenderMode.ScreenSpaceOverlay;
		component.overrideSorting = true;
		component.sortingOrder = sortingOrder;
		CanvasScaler component2 = obj.GetComponent<CanvasScaler>();
		component2.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		component2.referenceResolution = new Vector2(2400f, 1080f);
		component2.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
		component2.matchWidthOrHeight = 0.5f;
		return component;
	}

	private static string ResolveTileName(BattleTileData data)
	{
		if (data == null || string.IsNullOrWhiteSpace(data.DisplayName))
		{
			return data?.Id ?? "Stone";
		}
		return data.DisplayName;
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

	private static string ResolveTileSkill(BattleTileData data)
	{
		if (data?.Skill != null && !string.IsNullOrWhiteSpace(data.Skill.Name))
		{
			return data.Skill.Name;
		}
		if (data?.ActiveBonus != null && data.ActiveBonus.HasAnyBonus())
		{
			return T("Активный навык", "Active skill", "Aktif yetenek", "Aktive Fertigkeit");
		}
		return T("Пассивная сила", "Passive power", "Pasif güç", "Passive Kraft");
	}

	private static void ClearNamedChildren(Transform parent, params string[] prefixes)
	{
		for (int num = parent.childCount - 1; num >= 0; num--)
		{
			Transform child = parent.GetChild(num);
			for (int i = 0; i < prefixes.Length; i++)
			{
				if (child.name.StartsWith(prefixes[i], StringComparison.Ordinal))
				{
					UnityEngine.Object.Destroy(child.gameObject);
					break;
				}
			}
		}
	}

	private static GameObject CreatePanel(Transform parent, string objectName, Vector2 size, Vector2 position)
	{
		GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = size;
		if (!BattlePopupStyle.ApplyWindow(gameObject.GetComponent<Image>()))
		{
			gameObject.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.98f);
		}
		return gameObject;
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
		BattlePopupStyle.ApplyButton(component2, preserveCurrentColor: true);
		if (!string.IsNullOrEmpty(label))
		{
			CreateText(gameObject.transform, "Label", label, Vector2.zero, size, Mathf.Clamp(size.y * 0.42f, 20f, 58f), TextAlignmentOptions.Center).raycastTarget = false;
		}
		return component2;
	}

	private static Image CreateImage(Transform parent, string objectName, Vector2 position, Vector2 size)
	{
		GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		obj.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = obj.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = position;
		component.sizeDelta = size;
		return obj.GetComponent<Image>();
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
		component2.fontSizeMin = Mathf.Max(11f, fontSize * 0.5f);
		component2.fontSizeMax = fontSize;
		component2.alignment = alignment;
		component2.color = Color.white;
		component2.textWrappingMode = TextWrappingModes.Normal;
		BattlePopupStyle.ApplyText(component2);
		return component2;
	}

	private static string T(string russian, string english, string turkish, string german = null)
	{
		return BattleLoreTutorialSession.T(russian, english, turkish, german);
	}
}
}
