using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    public static class BattleDailyHeroBonusService
    {
        public struct DailyHeroBonus
        {
            public BattleCharacterDatabase.BattleCharacterData Character;
            public BattleCharacterDatabase.CharacterAnimalType AnimalType;
            public string Title;
            public string Subtitle;
            public string LoreText;
            public string BonusText;
            public BattleStatsHub.BattleStatsSnapshot Bonus;
            public DateTime ActiveDate;
            public TimeSpan TimeLeft;
            public bool IsBoostActive;
            public TimeSpan BoostTimeLeft;
            public bool IsValid => Character != null;
        }

        private const int CycleSeedSalt = 71237;
        private const string BoostEndTicksKey = "MahjongGame.Battle.DailyHeroBoostEndTicks";
        private const float RewardedBoostMultiplier = 1.5f;
        private static readonly DateTime Epoch = new DateTime(2026, 1, 1);

        public static bool TryGetTodayBonus(out DailyHeroBonus bonus)
        {
            bonus = default(DailyHeroBonus);

            if (!TryResolveDatabase(out BattleCharacterDatabase database))
                return false;

            List<BattleCharacterDatabase.BattleCharacterData> characters = database.GetEnabledCharacters();
            if (characters == null || characters.Count == 0)
                return false;

            List<BattleCharacterDatabase.CharacterAnimalType> animalTypes = GetEnabledAnimalTypes(characters);
            if (animalTypes.Count == 0)
                return false;

            DateTime now = DateTime.Now;
            DateTime today = now.Date;
            int dayIndex = Mathf.Max(0, (int)(today - Epoch).TotalDays);
            int cycleLength = animalTypes.Count;
            int cycleIndex = dayIndex / cycleLength;
            int positionInCycle = dayIndex % cycleLength;

            List<BattleCharacterDatabase.CharacterAnimalType> shuffled = BuildShuffledCycle(animalTypes, cycleIndex);
            BattleCharacterDatabase.CharacterAnimalType activeAnimal =
                shuffled[Mathf.Clamp(positionInCycle, 0, shuffled.Count - 1)];
            BattleCharacterDatabase.BattleCharacterData active = ResolveDisplayCharacter(database, activeAnimal, cycleIndex);
            if (active == null)
                return false;

            bool ownsActiveHero = PlayerOwnsBreed(activeAnimal);
            TimeSpan boostTimeLeft = TimeSpan.Zero;
            bool boostActive = ownsActiveHero && IsRewardedBoostActive(out boostTimeLeft);
            BattleStatsHub.BattleStatsSnapshot statBonus = ScaleBonus(GetStatBonus(active), boostActive ? RewardedBoostMultiplier : 1f);
            bonus = new DailyHeroBonus
            {
                Character = active,
                AnimalType = activeAnimal,
                Title = BuildTitle(active),
                Subtitle = BuildSubtitle(active),
                LoreText = BuildLoreText(active),
                BonusText = BuildBonusText(statBonus),
                Bonus = statBonus,
                ActiveDate = today,
                TimeLeft = today.AddDays(1) - now,
                IsBoostActive = boostActive,
                BoostTimeLeft = boostTimeLeft
            };

            return true;
        }

        public static bool IsTodayHero(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            if (!TryResolveCharacter(characterId, out BattleCharacterDatabase.BattleCharacterData character))
                return false;

            return TryGetTodayBonus(out DailyHeroBonus bonus)
                   && bonus.Character != null
                   && bonus.AnimalType == character.AnimalType;
        }

        public static BattleStatsHub.BattleStatsSnapshot ApplyTodayBonus(
            BattleStatsHub.BattleStatsSnapshot baseStats,
            string characterId)
        {
            return ApplyTodayBonus(baseStats, characterId, true);
        }

        public static BattleStatsHub.BattleStatsSnapshot ApplyTodayBonus(
            BattleStatsHub.BattleStatsSnapshot baseStats,
            string characterId,
            bool allowRewardedBoost)
        {
            if (!IsTodayHero(characterId) || !TryGetTodayBonus(out DailyHeroBonus bonus))
                return baseStats;

            BattleStatsHub.BattleStatsSnapshot rawBonus = GetStatBonus(bonus.Character);
            bool canUseRewardedBoost = allowRewardedBoost
                                       && PlayerOwnsBreed(bonus.AnimalType)
                                       && IsRewardedBoostActive(out _);
            BattleStatsHub.BattleStatsSnapshot scaledBonus = ScaleBonus(rawBonus, canUseRewardedBoost ? RewardedBoostMultiplier : 1f);

            return ApplyBonus(baseStats, scaledBonus);
        }

        public static BattleStatsHub.BattleStatsSnapshot ApplyTodayBonus(
            BattleStatsHub.BattleStatsSnapshot baseStats,
            BattleCharacterDatabase.BattleCharacterData character)
        {
            return character == null ? baseStats : ApplyTodayBonus(baseStats, character.Id);
        }

        public static void ActivateRewardedBoostForOneHour()
        {
            DateTime endTime = DateTime.Now.AddHours(1);
            PlayerPrefs.SetString(BoostEndTicksKey, endTime.Ticks.ToString());
            PlayerPrefs.Save();
        }

        public static bool CanUseRewardedBoostForToday()
        {
            return TryGetTodayBonus(out DailyHeroBonus bonus)
                   && bonus.Character != null
                   && PlayerOwnsBreed(bonus.AnimalType);
        }

        public static bool IsRewardedBoostActive()
        {
            return IsRewardedBoostActive(out _);
        }

        public static bool IsRewardedBoostActive(out TimeSpan timeLeft)
        {
            timeLeft = TimeSpan.Zero;

            string saved = PlayerPrefs.GetString(BoostEndTicksKey, string.Empty);
            if (string.IsNullOrWhiteSpace(saved) || !long.TryParse(saved, out long ticks))
                return false;

            DateTime endTime = new DateTime(ticks);
            timeLeft = endTime - DateTime.Now;

            if (timeLeft.TotalSeconds > 0)
                return true;

            PlayerPrefs.DeleteKey(BoostEndTicksKey);
            PlayerPrefs.Save();
            timeLeft = TimeSpan.Zero;
            return false;
        }

        private static bool PlayerOwnsBreed(BattleCharacterDatabase.CharacterAnimalType animalType)
        {
            if (!BattleCharacterSelectionService.HasInstance || !TryResolveDatabase(out BattleCharacterDatabase database))
                return false;

            BattleCharacterDatabase.BattleCharacterData male =
                database.FindByAnimalAndGender(animalType, BattleCharacterDatabase.CharacterGender.Male);
            if (male != null && BattleCharacterSelectionService.Instance.IsUnlocked(male.Id))
                return true;

            BattleCharacterDatabase.BattleCharacterData female =
                database.FindByAnimalAndGender(animalType, BattleCharacterDatabase.CharacterGender.Female);
            return female != null && BattleCharacterSelectionService.Instance.IsUnlocked(female.Id);
        }

        private static bool TryResolveCharacter(string characterId, out BattleCharacterDatabase.BattleCharacterData character)
        {
            character = null;
            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            if (!TryResolveDatabase(out BattleCharacterDatabase database))
                return false;

            character = database.GetCharacterOrNull(characterId.Trim());
            return character != null;
        }

        public static string FormatTimeLeft(TimeSpan timeLeft)
        {
            if (timeLeft.TotalSeconds <= 0)
                return "00:00";

            int hours = Mathf.Max(0, (int)timeLeft.TotalHours);
            int minutes = Mathf.Max(0, timeLeft.Minutes);
            return $"{hours:00}:{minutes:00}";
        }

        private static List<BattleCharacterDatabase.CharacterAnimalType> GetEnabledAnimalTypes(
            List<BattleCharacterDatabase.BattleCharacterData> characters)
        {
            List<BattleCharacterDatabase.CharacterAnimalType> result =
                new List<BattleCharacterDatabase.CharacterAnimalType>();

            for (int i = 0; i < characters.Count; i++)
            {
                BattleCharacterDatabase.BattleCharacterData character = characters[i];
                if (character == null || !character.IsEnabled || result.Contains(character.AnimalType))
                    continue;

                result.Add(character.AnimalType);
            }

            return result;
        }

        private static List<BattleCharacterDatabase.CharacterAnimalType> BuildShuffledCycle(
            List<BattleCharacterDatabase.CharacterAnimalType> source,
            int cycleIndex)
        {
            List<BattleCharacterDatabase.CharacterAnimalType> result =
                new List<BattleCharacterDatabase.CharacterAnimalType>(source);

            System.Random random = new System.Random(CycleSeedSalt + cycleIndex * 1009);
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                BattleCharacterDatabase.CharacterAnimalType temp = result[i];
                result[i] = result[j];
                result[j] = temp;
            }

            if (result.Count > 1)
            {
                List<BattleCharacterDatabase.CharacterAnimalType> previous =
                    new List<BattleCharacterDatabase.CharacterAnimalType>(source);
                System.Random previousRandom = new System.Random(CycleSeedSalt + (cycleIndex - 1) * 1009);
                for (int i = previous.Count - 1; i > 0; i--)
                {
                    int j = previousRandom.Next(i + 1);
                    BattleCharacterDatabase.CharacterAnimalType temp = previous[i];
                    previous[i] = previous[j];
                    previous[j] = temp;
                }

                if (previous.Count > 0 && result[0] == previous[previous.Count - 1])
                {
                    BattleCharacterDatabase.CharacterAnimalType temp = result[0];
                    result[0] = result[1];
                    result[1] = temp;
                }
            }

            return result;
        }

        private static BattleCharacterDatabase.BattleCharacterData ResolveDisplayCharacter(
            BattleCharacterDatabase database,
            BattleCharacterDatabase.CharacterAnimalType animalType,
            int cycleIndex)
        {
            BattleCharacterDatabase.CharacterGender preferredGender = cycleIndex % 2 == 0
                ? BattleCharacterDatabase.CharacterGender.Male
                : BattleCharacterDatabase.CharacterGender.Female;
            BattleCharacterDatabase.CharacterGender fallbackGender = preferredGender == BattleCharacterDatabase.CharacterGender.Male
                ? BattleCharacterDatabase.CharacterGender.Female
                : BattleCharacterDatabase.CharacterGender.Male;

            return database.FindByAnimalAndGender(animalType, preferredGender)
                   ?? database.FindByAnimalAndGender(animalType, fallbackGender);
        }

        private static bool TryResolveDatabase(out BattleCharacterDatabase database)
        {
            database = BattleCharacterDatabase.HasInstance
                ? BattleCharacterDatabase.Instance
                : UnityEngine.Object.FindAnyObjectByType<BattleCharacterDatabase>(FindObjectsInactive.Include);
            return database != null;
        }

        private static BattleStatsHub.BattleStatsSnapshot GetStatBonus(
            BattleCharacterDatabase.BattleCharacterData character)
        {
            if (character == null)
                return new BattleStatsHub.BattleStatsSnapshot(1, 0, 0f, 0f, 0f, 1f);

            switch (character.AnimalType)
            {
                case BattleCharacterDatabase.CharacterAnimalType.Bear:
                    return new BattleStatsHub.BattleStatsSnapshot(35, 0, 0.02f, 0.02f, 0f, 1f);
                case BattleCharacterDatabase.CharacterAnimalType.Tiger:
                    return new BattleStatsHub.BattleStatsSnapshot(1, 3, 0f, 0f, 0.03f, 1f);
                case BattleCharacterDatabase.CharacterAnimalType.Wolf:
                    return new BattleStatsHub.BattleStatsSnapshot(1, 3, 0f, 0.03f, 0f, 1f);
                case BattleCharacterDatabase.CharacterAnimalType.Fox:
                    return new BattleStatsHub.BattleStatsSnapshot(1, 0, 0f, 0f, 0.08f, 1.20f);
                case BattleCharacterDatabase.CharacterAnimalType.Dragon:
                    return new BattleStatsHub.BattleStatsSnapshot(25, 3, 0f, 0f, 0f, 1f);
                case BattleCharacterDatabase.CharacterAnimalType.Dog:
                    return new BattleStatsHub.BattleStatsSnapshot(30, 0, 0.03f, 0f, 0f, 1f);
                default:
                    return new BattleStatsHub.BattleStatsSnapshot(25, 2, 0f, 0f, 0f, 1f);
            }
        }

        private static BattleStatsHub.BattleStatsSnapshot ScaleBonus(
            BattleStatsHub.BattleStatsSnapshot bonus,
            float multiplier)
        {
            multiplier = Mathf.Max(1f, multiplier);
            return new BattleStatsHub.BattleStatsSnapshot(
                bonus.MaxHp > 1 ? Mathf.RoundToInt(bonus.MaxHp * multiplier) : 1,
                Mathf.RoundToInt(bonus.Attack * multiplier),
                bonus.Armor * multiplier,
                bonus.ParryChance * multiplier,
                bonus.CritChance * multiplier,
                bonus.CritDamageMultiplier > 1f
                    ? 1f + (bonus.CritDamageMultiplier - 1f) * multiplier
                    : 1f);
        }

        private static BattleStatsHub.BattleStatsSnapshot ApplyBonus(
            BattleStatsHub.BattleStatsSnapshot baseStats,
            BattleStatsHub.BattleStatsSnapshot bonus)
        {
            return new BattleStatsHub.BattleStatsSnapshot(
                baseStats.MaxHp + (bonus.MaxHp > 1 ? bonus.MaxHp : 0),
                baseStats.Attack + Mathf.Max(0, bonus.Attack),
                Mathf.Clamp01(baseStats.Armor + Mathf.Max(0f, bonus.Armor)),
                Mathf.Clamp01(baseStats.ParryChance + Mathf.Max(0f, bonus.ParryChance)),
                Mathf.Clamp01(baseStats.CritChance + Mathf.Max(0f, bonus.CritChance)),
                Mathf.Max(1f, baseStats.CritDamageMultiplier + Mathf.Max(0f, bonus.CritDamageMultiplier - 1f)));
        }

        private static string BuildTitle(BattleCharacterDatabase.BattleCharacterData character)
        {
            string name = BattleCharacterDatabase.GetLocalizedDisplayName(character);
            return string.IsNullOrWhiteSpace(name)
                ? GameLocalization.Text("battle.daily.title")
                : GameLocalization.Format("battle.daily.title_named", name);
        }

        private static string BuildSubtitle(BattleCharacterDatabase.BattleCharacterData character)
        {
            if (character == null)
                return GameLocalization.Text("battle.daily.silent");

            return GameLocalization.Format("battle.daily.subtitle", BattleCharacterDatabase.GetLocalizedDisplayName(character));
        }

        private static string BuildLoreText(BattleCharacterDatabase.BattleCharacterData character)
        {
            if (character == null)
                return GameLocalization.Text("battle.daily.no_sign");

            string name = BattleCharacterDatabase.GetLocalizedDisplayName(character);
            string clan = GetAnimalDisplayName(character.AnimalType);

            switch (character.AnimalType)
            {
                case BattleCharacterDatabase.CharacterAnimalType.Bear:
                    return GameLocalization.Format("battle.daily.lore.bear", name, clan);
                case BattleCharacterDatabase.CharacterAnimalType.Tiger:
                    return GameLocalization.Format("battle.daily.lore.tiger", name, clan);
                case BattleCharacterDatabase.CharacterAnimalType.Wolf:
                    return GameLocalization.Format("battle.daily.lore.wolf", name, clan);
                case BattleCharacterDatabase.CharacterAnimalType.Fox:
                    return GameLocalization.Format("battle.daily.lore.fox", name, clan);
                case BattleCharacterDatabase.CharacterAnimalType.Dragon:
                    return GameLocalization.Format("battle.daily.lore.dragon", name, clan);
                case BattleCharacterDatabase.CharacterAnimalType.Dog:
                    return GameLocalization.Format("battle.daily.lore.dog", name, clan);
                default:
                    return GameLocalization.Format("battle.daily.lore.default", name);
            }
        }

        private static string BuildBonusText(BattleStatsHub.BattleStatsSnapshot bonus)
        {
            List<string> lines = new List<string>();
            if (bonus.MaxHp > 1)
                lines.Add(GameLocalization.Format("battle.daily.bonus.hp", bonus.MaxHp));
            if (bonus.Attack > 0)
                lines.Add(GameLocalization.Format("battle.daily.bonus.attack", bonus.Attack));
            if (bonus.Armor > 0f)
                lines.Add(GameLocalization.Format("battle.daily.bonus.armor", Mathf.RoundToInt(bonus.Armor * 100f)));
            if (bonus.CritChance > 0f)
                lines.Add(GameLocalization.Format("battle.daily.bonus.crit", Mathf.RoundToInt(bonus.CritChance * 100f)));
            if (bonus.CritDamageMultiplier > 1f)
                lines.Add(GameLocalization.Format("battle.daily.bonus.crit_damage", bonus.CritDamageMultiplier - 1f));

            return lines.Count == 0 ? GameLocalization.Text("battle.daily.bonus_none") : string.Join("\n", lines);
        }

        private static string GetAnimalDisplayName(BattleCharacterDatabase.CharacterAnimalType animalType)
        {
            switch (animalType)
            {
                case BattleCharacterDatabase.CharacterAnimalType.Tiger:
                    return GameLocalization.Text("battle.daily.clan.tiger");
                case BattleCharacterDatabase.CharacterAnimalType.Fox:
                    return GameLocalization.Text("battle.daily.clan.fox");
                case BattleCharacterDatabase.CharacterAnimalType.Wolf:
                    return GameLocalization.Text("battle.daily.clan.wolf");
                case BattleCharacterDatabase.CharacterAnimalType.Bear:
                    return GameLocalization.Text("battle.daily.clan.bear");
                case BattleCharacterDatabase.CharacterAnimalType.Dragon:
                    return GameLocalization.Text("battle.daily.clan.dragon");
                case BattleCharacterDatabase.CharacterAnimalType.Dog:
                    return GameLocalization.Text("battle.daily.clan.dog");
                default:
                    return GameLocalization.Text("battle.daily.clan.default");
            }
        }
    }
}
