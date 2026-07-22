using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{

public static class BattleLoreTutorialSession
{
	public const int ChapterNumber = 1;

	public const int StageCount = 6;

	public const int RemovedParryStage = 2;

	public const int PlayableStageCount = StageCount - 1;

	private const string ActivePrefsKey = "MahjongBattleLoreTutorial.Active";

	private const string StagePrefsKey = "MahjongBattleLoreTutorial.Stage";

	private const string CompletedStagePrefsKey = "MahjongBattleLoreTutorial.CompletedStage";

	private const string RewardClaimedPrefsKey = "MahjongBattleLoreTutorial.Chapter1RewardClaimed";

	private const string OpenOnLobbyReturnPrefsKey = "MahjongBattleLoreTutorial.OpenOnLobbyReturn";

	private const string StageThreeRewardTilePrefsKey = "MahjongBattleLoreTutorial.Stage3RewardTile";

	public static bool IsActive => PlayerPrefs.GetInt(GetScopedPrefsKey("MahjongBattleLoreTutorial.Active"), 0) == 1;

	public static int ActiveStage => NormalizePlayableStage(PlayerPrefs.GetInt(GetScopedPrefsKey("MahjongBattleLoreTutorial.Stage"), 1));

	public static int CompletedStage => NormalizeCompletedStage(PlayerPrefs.GetInt(GetScopedPrefsKey("MahjongBattleLoreTutorial.CompletedStage"), 0));

	public static bool RewardClaimed => PlayerPrefs.GetInt(GetScopedPrefsKey("MahjongBattleLoreTutorial.Chapter1RewardClaimed"), 0) == 1;

	public static bool ShouldOpenOnLobbyReturn => PlayerPrefs.GetInt(GetScopedPrefsKey("MahjongBattleLoreTutorial.OpenOnLobbyReturn"), 0) == 1;

	public static bool IsTrainingComplete => CompletedStage >= 6;

	public static void BeginStage(int stage)
	{
		PlayerPrefs.SetInt(GetScopedPrefsKey("MahjongBattleLoreTutorial.Active"), 1);
		PlayerPrefs.SetInt(GetScopedPrefsKey("MahjongBattleLoreTutorial.Stage"), NormalizePlayableStage(stage));
		PlayerPrefs.Save();
	}

	public static void ClearActive()
	{
		PlayerPrefs.SetInt(GetScopedPrefsKey("MahjongBattleLoreTutorial.Active"), 0);
		PlayerPrefs.Save();
	}

	public static void CompleteActiveStage()
	{
		if (IsActive)
		{
			int value = Mathf.Max(CompletedStage, ActiveStage);
			PlayerPrefs.SetInt(GetScopedPrefsKey("MahjongBattleLoreTutorial.CompletedStage"), Mathf.Clamp(value, 0, 6));
			PlayerPrefs.Save();
		}
	}

	public static void MarkRewardClaimed()
	{
		PlayerPrefs.SetInt(GetScopedPrefsKey("MahjongBattleLoreTutorial.Chapter1RewardClaimed"), 1);
		PlayerPrefs.Save();
	}

	[Obsolete("Tutorial test reset has been removed from the project.")]
	public static void ResetStageForTesting(int stage)
	{
		Debug.LogWarning("[BattleLoreTutorialSession] Tutorial test reset was removed and cannot change player progress.");
	}

	public static void RequestOpenOnLobbyReturn()
	{
		PlayerPrefs.SetInt(GetScopedPrefsKey("MahjongBattleLoreTutorial.OpenOnLobbyReturn"), 1);
		PlayerPrefs.Save();
	}

	public static bool ConsumeOpenOnLobbyReturn()
	{
		string scopedPrefsKey = GetScopedPrefsKey("MahjongBattleLoreTutorial.OpenOnLobbyReturn");
		if (PlayerPrefs.GetInt(scopedPrefsKey, 0) != 1)
		{
			return false;
		}
		PlayerPrefs.SetInt(scopedPrefsKey, 0);
		PlayerPrefs.Save();
		return true;
	}

	private static string GetScopedPrefsKey(string baseKey)
	{
		string currentProfileScopeKey = GetCurrentProfileScopeKey();
		if (!string.IsNullOrWhiteSpace(currentProfileScopeKey))
		{
			return baseKey + "." + currentProfileScopeKey;
		}
		return baseKey;
	}

	public static bool IsStageRemoved(int stage)
	{
		return Mathf.Clamp(stage, 1, 6) == RemovedParryStage;
	}

	public static int GetDisplayStageNumber(int stage)
	{
		int normalized = NormalizePlayableStage(stage);
		return normalized > RemovedParryStage ? normalized - 1 : normalized;
	}

	public static int NormalizePlayableStage(int stage)
	{
		int normalized = Mathf.Clamp(stage, 1, 6);
		return IsStageRemoved(normalized) ? 3 : normalized;
	}

	public static int NormalizeCompletedStage(int stage)
	{
		int normalized = Mathf.Clamp(stage, 0, 6);
		return normalized == 1 ? 2 : normalized;
	}

	public static int GetNextPlayableStage(int stage)
	{
		int normalized = Mathf.Clamp(stage, 1, 6);
		while (normalized <= 6 && IsStageRemoved(normalized))
		{
			normalized++;
		}
		return Mathf.Clamp(normalized, 1, 6);
	}

	public static int GetPreviousPlayableStage(int stage)
	{
		int normalized = Mathf.Clamp(stage, 1, 6);
		while (normalized >= 1 && IsStageRemoved(normalized))
		{
			normalized--;
		}
		return Mathf.Clamp(normalized, 1, 6);
	}

	private static string GetCurrentProfileScopeKey()
	{
		PlayerProfile playerProfile = ((ProfileService.I != null) ? ProfileService.I.Current : null);
		if (playerProfile == null)
		{
			return string.Empty;
		}
		if (!string.IsNullOrWhiteSpace(playerProfile.OnlinePlayerId))
		{
			return "account_" + SanitizePrefsScope(playerProfile.OnlinePlayerId);
		}
		if (!string.IsNullOrWhiteSpace(playerProfile.LocalProfileId))
		{
			return "profile_" + SanitizePrefsScope(playerProfile.LocalProfileId);
		}
		return string.Empty;
	}

	private static string SanitizePrefsScope(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}
		string text = value.Trim();
		char[] array = new char[text.Length];
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			array[i] = (char.IsLetterOrDigit(c) ? c : '_');
		}
		return new string(array);
	}

	public static IReadOnlyList<BattleTileData> GetTrialDeckTiles(BattleTileStore store, int targetCount)
	{
		List<BattleTileData> list = new List<BattleTileData>();
		if (store == null || store.BattleTiles == null || store.BattleTiles.Count == 0)
		{
			return list;
		}
		List<BattleTileData> list2 = new List<BattleTileData>();
		for (int i = 0; i < store.BattleTiles.Count; i++)
		{
			BattleTileData battleTileData = store.BattleTiles[i];
			if (battleTileData != null && !(battleTileData.Prefab == null) && !string.IsNullOrWhiteSpace(battleTileData.Id))
			{
				list2.Add(battleTileData);
			}
		}
		list2.Sort(CompareTrialTiles);
		int num = Mathf.Min(list2.Count, Mathf.Clamp(12, 2, Mathf.Max(2, targetCount)));
		while (list.Count < targetCount && num > 0)
		{
			for (int j = 0; j < num; j++)
			{
				if (list.Count >= targetCount)
				{
					break;
				}
				list.Add(list2[j]);
			}
		}
		if ((list.Count & 1) != 0)
		{
			list.RemoveAt(list.Count - 1);
		}
		return list;
	}

	public static BattleRoundConfig GetBattleRoundConfigForActiveStage(int roundIndex)
	{
		int activeStage = NormalizePlayableStage(ActiveStage);
		return new BattleRoundConfig
		{
			RoundIndex = Mathf.Max(1, roundIndex),
			LayoutLevel = activeStage,
			TilesToUse = GetTutorialTileCount(activeStage)
		};
	}

	public static List<LayoutSlot> GetTutorialLayoutSlots(int stage)
	{
		stage = NormalizePlayableStage(stage);
		return stage switch
		{
			1 => RectangleSlots(2, 2), 
			2 => RectangleSlots(3, 2), 
			3 => RectangleSlots(4, 2), 
			4 => RectangleSlots(5, 2), 
			5 => RectangleSlots(4, 3), 
			6 => RectangleSlots(5, 3, 14), 
			7 => RectangleSlots(6, 3), 
			8 => RectangleSlots(7, 3, 20), 
			9 => RectangleSlots(8, 3), 
			_ => RectangleSlots(9, 4), 
		};
	}

	public static int GetTutorialTileCount(int stage)
	{
		int num = GetTutorialLayoutSlots(stage)?.Count ?? 0;
		return Mathf.Max(2, num - (num & 1));
	}

	public static IReadOnlyList<BattleTileData> GetRewardCandidates(BattleTileStore store, BattleTileRarity rarity, int maxCount)
	{
		List<BattleTileData> list = new List<BattleTileData>();
		if (store == null || store.BattleTiles == null)
		{
			return list;
		}
		for (int i = 0; i < store.BattleTiles.Count; i++)
		{
			BattleTileData battleTileData = store.BattleTiles[i];
			if (battleTileData != null && !(battleTileData.Prefab == null) && !string.IsNullOrWhiteSpace(battleTileData.Id) && battleTileData.Rarity == rarity)
			{
				list.Add(battleTileData);
			}
		}
		list.Sort(CompareRewardTiles);
		if (list.Count > maxCount)
		{
			list.RemoveRange(maxCount, list.Count - maxCount);
		}
		return list;
	}

	public static string GetStageTitle(int stage)
	{
		return NormalizePlayableStage(stage) switch
		{
			1 => T("Первый удар", "First Strike", "İlk Vuruş", "Erster Schlag"), 
			3 => T("Кожа пепла", "Ash Skin", "Kül Derisi", "Aschehaut"), 
			4 => T("Кузница трех камней", "Three-Stone Forge", "Üç Taş Forge", "Drei-Stein-Forge"), 
			5 => T("Камень тотема", "Totem Stone", "Totem Taşı", "Totemstein"), 
			_ => T("FULL экран", "FULL Screen", "FULL Ekran", "FULL-Bildschirm"), 
		};
	}

	public static string GetStageLesson(int stage)
	{
		return NormalizePlayableStage(stage) switch
		{
			1 => T("Найди две пары: первый удар покажет HP-бары, второй завершит бой.", "Find two pairs: the first hit reveals HP bars, the second ends the fight.", "İki eş bul: ilk vuruş HP barlarını açar, ikinci savaşı bitirir.", "Finde zwei Paare: Der erste Treffer zeigt HP-Leisten, der zweite beendet den Kampf."), 
			3 => T("Armor режет часть урона, critical умножает удар. Следи за числом в скобках и меткой Crit!", "Armor cuts part of incoming damage, critical multiplies a hit. Watch the number in brackets and the Crit! label.", "Armor hasarın bir kısmını keser, critical vuruşu çarpar. Parantezdeki sayıya ve Crit! yazısına bak.", "Armor reduziert Schaden, Critical verstaerkt Treffer. Achte auf die Zahl in Klammern und Crit!."), 
			4 => T("Открой кузницу в лобби и соедини 3 одинаковых редких камня в один усиленный.", "Open Forge in the lobby and combine 3 identical rare stones into one upgraded stone.", "Lobide Forge aç ve 3 aynı rare taşı tek güçlendirilmiş taşa dönüştür.", "Oeffne Forge in der Lobby und verbinde 3 gleiche Rare-Steine zu einem verstaerkten Stein."), 
			5 => T("Открой сумку и назначь тотемом один из активных камней. Он останется в наборе и будет участвовать в бою.", "Open the Bag and assign one of the active stones as the Totem. It stays in the loadout and participates in battle.", "Çantayı aç ve aktif taşlardan birini Totem olarak ata. Taş destede kalır ve savaşa katılır.", "Öffne die Tasche und bestimme einen der aktiven Steine zum Totem. Er bleibt im Set und nimmt am Kampf teil."), 
			_ => T("Финал учит открывать бой на весь экран. Нажми FULL, посмотри большую доску и заверши бой за выбор редкого и эпического камня.", "The finale teaches the fullscreen battle view. Press FULL, read the larger board, then win to choose a rare and an epic stone.", "Final tam ekran savaş görünümünü öğretir. FULL'a bas, büyük tahtayı oku ve rare ile epic taş seçimi için kazan.", "Das Finale zeigt die Vollbild-Kampfansicht. Druecke FULL, lies das groessere Brett und gewinne fuer rare und epic."), 
		};
	}

	public static string GetStageRewardText(int stage)
	{
		return Mathf.Clamp(stage, 1, 6) switch
		{
			1 => T("+10 OzTile", "+10 OzTile", "+10 OzTile", "+10 OzTile"), 
			2 => T("+15 OzTile", "+15 OzTile", "+15 OzTile", "+15 OzTile"), 
			3 => T("+20 OzTile и 3 одинаковых редких камня", "+20 OzTile and 3 identical rare stones", "+20 OzTile ve 3 ayni nadir tas", "+20 OzTile und 3 gleiche seltene Steine"), 
			4 => T("+25 OzTile", "+25 OzTile", "+25 OzTile", "+25 OzTile"), 
			5 => T("+30 OzTile", "+30 OzTile", "+30 OzTile", "+30 OzTile"), 
			_ => T("+35 OzTile и выбор: 1 редкий + 1 эпический камень", "+35 OzTile and choice: 1 rare + 1 epic stone", "+35 OzTile ve seçim: 1 rare + 1 epic taş", "+35 OzTile und Wahl: 1 rare + 1 epic Stein"), 
		};
	}

	public static int GetStageOzTileReward(int stage)
	{
		return Mathf.Clamp(stage, 1, 6) switch
		{
			1 => 10, 
			2 => 15, 
			3 => 20, 
			4 => 25, 
			5 => 30, 
			6 => 35, 
			7 => 40, 
			8 => 45, 
			9 => 50, 
			_ => 0, 
		};
	}

	public static void GrantStageReward(int stage)
	{
		int stageOzTileReward = GetStageOzTileReward(stage);
		if (stageOzTileReward > 0)
		{
			if (CurrencyService.I != null)
			{
				CurrencyService.I.AddOzTile(stageOzTileReward);
			}
			else if (ProfileService.I != null && ProfileService.I.Current != null)
			{
				ProfileService.I.Current.AddTile(stageOzTileReward);
			}
		}
		if (stage == 3)
		{
			GrantStageThreeForgeStones((ProfileService.I != null) ? ProfileService.I.Current : null, (BattleTileStore.I != null) ? BattleTileStore.I : UnityEngine.Object.FindAnyObjectByType<BattleTileStore>());
		}
		if (ProfileService.I != null)
		{
			ProfileService.I.Save();
			ProfileService.I.NotifyProfileChanged();
		}
	}

	public static string GetStageThreeRewardTileId()
	{
		return PlayerPrefs.GetString(GetScopedPrefsKey("MahjongBattleLoreTutorial.Stage3RewardTile"), string.Empty);
	}

	public static string EnsureStageThreeRewardTilePrepared(BattleTileStore store)
	{
		string stageThreeRewardTileId = GetStageThreeRewardTileId();
		if (!string.IsNullOrWhiteSpace(stageThreeRewardTileId) && store != null && store.TryGetTileDataById(stageThreeRewardTileId, out var data) && data != null && data.Rarity == BattleTileRarity.Rare)
		{
			return stageThreeRewardTileId;
		}
		BattleTileData battleTileData = PickRandomRareRewardTile(store);
		if (battleTileData == null || string.IsNullOrWhiteSpace(battleTileData.Id))
		{
			return string.Empty;
		}
		PlayerPrefs.SetString(GetScopedPrefsKey("MahjongBattleLoreTutorial.Stage3RewardTile"), battleTileData.Id);
		PlayerPrefs.Save();
		return battleTileData.Id;
	}

	public static string GetStageThreeRewardTileName(BattleTileStore store)
	{
		string text = EnsureStageThreeRewardTilePrepared(store);
		if (!string.IsNullOrWhiteSpace(text) && store != null && store.TryGetTileDataById(text, out var data) && data != null)
		{
			return ResolveRewardTileName(data, text);
		}
		return T("редкий камень", "rare stone", "nadir tas", "seltener Stein");
	}

	private static void GrantStageThreeForgeStones(PlayerProfile profile, BattleTileStore store)
	{
		if (profile == null || store == null || CompletedStage >= 3)
		{
			return;
		}
		BattleTileInventoryService.EnsureInventoryForStore(profile, store);
		string text = EnsureStageThreeRewardTilePrepared(store);
		if (string.IsNullOrWhiteSpace(text) || !store.TryGetTileDataById(text, out var data) || data == null || data.Rarity != BattleTileRarity.Rare)
		{
			data = PickRandomRareRewardTile(store);
			text = ((data != null) ? data.Id : string.Empty);
		}
		if (data != null && !string.IsNullOrWhiteSpace(text))
		{
			PlayerPrefs.SetString(GetScopedPrefsKey("MahjongBattleLoreTutorial.Stage3RewardTile"), text);
			for (int i = 0; i < 3; i++)
			{
				BattleTileInventoryService.GrantTileCopy(profile, store, text, out var _);
			}
		}
	}

	private static BattleTileData PickRandomRareRewardTile(BattleTileStore store)
	{
		IReadOnlyList<BattleTileData> readOnlyList = ((store != null) ? store.BattleTiles : null);
		if (readOnlyList == null || readOnlyList.Count == 0)
		{
			return null;
		}
		List<BattleTileData> list = new List<BattleTileData>();
		for (int i = 0; i < readOnlyList.Count; i++)
		{
			BattleTileData battleTileData = readOnlyList[i];
			if (battleTileData != null && !(battleTileData.Prefab == null) && !string.IsNullOrWhiteSpace(battleTileData.Id) && battleTileData.Rarity == BattleTileRarity.Rare && !BattleTileInventoryService.IsBaseBattleTile(battleTileData.Id))
			{
				list.Add(battleTileData);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	private static string ResolveRewardTileName(BattleTileData data, string fallbackId)
	{
		if (data != null && !string.IsNullOrWhiteSpace(data.DisplayName))
		{
			return data.DisplayName.Trim();
		}
		if (!string.IsNullOrWhiteSpace(fallbackId))
		{
			return fallbackId.Trim();
		}
		return T("редкий камень", "rare stone", "nadir tas", "seltener Stein");
	}

	private static int CompareTrialTiles(BattleTileData a, BattleTileData b)
	{
		int num = b.Rarity.CompareTo(a.Rarity);
		if (num != 0)
		{
			return num;
		}
		int num2 = ScoreActive(b).CompareTo(ScoreActive(a));
		if (num2 == 0)
		{
			return string.Compare(a.Id, b.Id, StringComparison.Ordinal);
		}
		return num2;
	}

	private static int CompareRewardTiles(BattleTileData a, BattleTileData b)
	{
		int num = ScoreActive(b).CompareTo(ScoreActive(a));
		if (num == 0)
		{
			return string.Compare(a.Id, b.Id, StringComparison.Ordinal);
		}
		return num;
	}

	private static int ScoreActive(BattleTileData tile)
	{
		if (tile?.ActiveBonus == null)
		{
			return 0;
		}
		return tile.ActiveBonus.Attack + tile.ActiveBonus.HealSelf + Mathf.RoundToInt(tile.ActiveBonus.CritChance * 100f);
	}

	private static List<LayoutSlot> RectangleSlots(int columns, int rows, int maxSlots = int.MaxValue)
	{
		List<LayoutSlot> list = new List<LayoutSlot>(Mathf.Max(0, columns * rows));
		int num = columns / 2;
		int num2 = rows / 2;
		for (int num3 = rows - 1; num3 >= 0; num3--)
		{
			for (int i = 0; i < columns; i++)
			{
				if (list.Count >= maxSlots)
				{
					return list;
				}
				list.Add(new LayoutSlot
				{
					X = i - num,
					Y = num3 - num2,
					Z = 0
				});
			}
		}
		return list;
	}

	public static string T(string russian, string english, string turkish, string german = null)
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
