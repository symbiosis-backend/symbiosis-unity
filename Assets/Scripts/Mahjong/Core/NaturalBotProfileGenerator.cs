using UnityEngine;

namespace MahjongGame
{
    public enum NaturalBotLanguage
    {
        Russian = 0,
        English = 1,
        Turkish = 2,
        German = 3
    }

    public readonly struct NaturalBotProfile
    {
        public NaturalBotProfile(string nickname, NaturalBotLanguage language, string statusLine)
        {
            Nickname = nickname;
            Language = language;
            StatusLine = statusLine;
        }

        public string Nickname { get; }
        public NaturalBotLanguage Language { get; }
        public string StatusLine { get; }
    }

    public readonly struct NaturalChatLine
    {
        public NaturalChatLine(NaturalBotProfile profile, string text)
        {
            Profile = profile;
            Text = text;
        }

        public NaturalBotProfile Profile { get; }
        public string Text { get; }
    }

    public static class NaturalBotProfileGenerator
    {
        private static readonly string[] RussianNames =
        {
            "Sasha", "Misha", "Nika", "Alina", "Dima", "Vera", "Kirill", "Rita",
            "Anton", "Lera", "Egor", "Mila", "Pavel", "Yana", "Roma", "Kira",
            "Artem", "Nastya", "Vadim", "Olya", "Ilya", "Masha", "Timur", "Lina"
        };

        private static readonly string[] EnglishNames =
        {
            "Alex", "Maya", "Chris", "Nate", "Ivy", "Sam", "Tess", "Owen",
            "Jade", "Ryan", "Mia", "Noah", "Liam", "Ava", "Ben", "Skye",
            "Cole", "June", "Max", "Eli", "Lena", "Theo", "Rae", "Finn"
        };

        private static readonly string[] TurkishNames =
        {
            "Deniz", "Ece", "Kaan", "Elif", "Mert", "Aylin", "Arda", "Selin",
            "Bora", "Mina", "Emir", "Duru", "Kerem", "Lale", "Eren", "Asli",
            "Can", "Melis", "Tuna", "Yaren", "Ozan", "Nehir", "Baran", "Sena"
        };

        private static readonly string[] GermanNames =
        {
            "Lukas", "Mila", "Finn", "Lea", "Jonas", "Emma", "Noah", "Lina",
            "Ben", "Ella", "Paul", "Maja", "Felix", "Nora", "Max", "Clara",
            "Theo", "Hanna", "Leon", "Ida", "Luis", "Sofia", "Erik", "Leni"
        };

        private static readonly string[] Handles =
        {
            "tileflow", "quiettable", "lastpair", "eastwind", "slowhand", "deepread",
            "calmqueue", "nightmatch", "softshuffle", "riverline", "jadewall", "afterwork",
            "coffeehand", "tabletime", "rankgrind", "latejoin", "cleanpair", "gooddraw",
            "warmup", "closegame", "dailywin", "matchready", "fastthink", "steadyplay"
        };

        private static readonly string[] StatusRussian =
        {
            "разминается перед партией", "ищет спокойный матч", "играет после работы",
            "копит серию побед", "тренирует быстрые пары", "вернулся в рейтинговый режим",
            "ждет нормальный стол", "проверяет новую тактику"
        };

        private static readonly string[] StatusEnglish =
        {
            "warming up before a match", "looking for a fair table", "playing after work",
            "trying to keep a win streak", "testing a new opening", "back in ranked",
            "waiting for a clean game", "practicing fast pairs"
        };

        private static readonly string[] StatusTurkish =
        {
            "mac oncesi isiniyor", "sakin bir masa ariyor", "isten sonra oynuyor",
            "galibiyet serisini koruyor", "yeni acilisi deniyor", "ranked moduna dondu",
            "temiz bir oyun bekliyor", "hizli esleri calisiyor"
        };

        private static readonly string[] StatusGerman =
        {
            "warmt sich vor dem Match auf", "sucht einen fairen Tisch", "spielt nach der Arbeit",
            "will die Siegesserie halten", "testet eine neue Eroffnung", "ist zuruck im Ranked",
            "wartet auf ein sauberes Spiel", "ubt schnelle Paare"
        };

        private static readonly string[] GlobalRussianLines =
        {
            "Всем привет, кто сейчас в игре?",
            "Зашел на пару минут, посмотрю что тут нового.",
            "У кого сегодня нормально грузит лобби?",
            "После обновления стало приятнее заходить.",
            "Я пока в меню, если что зовите в матч.",
            "Проверяю профиль и дальше в бой."
        };

        private static readonly string[] GlobalEnglishLines =
        {
            "Hey, anyone playing right now?",
            "Just checking the lobby before a match.",
            "The menu feels smoother today.",
            "I am around for one quick game.",
            "Good luck to everyone queueing.",
            "Trying the new build for a bit."
        };

        private static readonly string[] GlobalTurkishLines =
        {
            "Selam, su an oynayan var mi?",
            "Maca girmeden once lobiyi kontrol ediyorum.",
            "Bugun menu daha akici gibi.",
            "Kisa bir oyun icin buradayim.",
            "Siraya giren herkese bol sans.",
            "Yeni surumu biraz deniyorum."
        };

        private static readonly string[] GlobalGermanLines =
        {
            "Hey, spielt gerade jemand?",
            "Ich schaue kurz in die Lobby vor dem Match.",
            "Das Menu fuhlt sich heute flussiger an.",
            "Ich bin fur ein schnelles Spiel da.",
            "Viel Gluck an alle in der Warteschlange.",
            "Ich teste den neuen Build ein bisschen."
        };

        private static readonly string[] MahjongRussianLines =
        {
            "Кто в маджонг на быстрый матч?",
            "Сейчас бы короткую партию без спешки.",
            "Пару раундов сыграю, потом уйду.",
            "Ищу соперника примерно моего уровня.",
            "Сегодня пары собираются странно, но интересно.",
            "Перед рейтинговым хочу размяться."
        };

        private static readonly string[] MahjongEnglishLines =
        {
            "Anyone up for a quick mahjong match?",
            "I can play one short round.",
            "Looking for someone around my rank.",
            "Warming up before ranked.",
            "The pairs feel tricky today.",
            "I will queue after this message."
        };

        private static readonly string[] MahjongTurkishLines =
        {
            "Hizli bir mahjong maci isteyen var mi?",
            "Bir kisa tur oynayabilirim.",
            "Benim seviyeme yakin rakip ariyorum.",
            "Ranked oncesi biraz isiniyorum.",
            "Bugun esler biraz ters geliyor.",
            "Bu mesajdan sonra siraya girecegim."
        };

        private static readonly string[] MahjongGermanLines =
        {
            "Jemand Lust auf ein schnelles Mahjong-Match?",
            "Ich kann eine kurze Runde spielen.",
            "Suche jemanden ungefahr auf meinem Rang.",
            "Ich warme mich vor Ranked auf.",
            "Die Paare sind heute knifflig.",
            "Nach dieser Nachricht gehe ich in die Queue."
        };

        public static NaturalBotProfile CreateProfile(string rankTier = null)
        {
            NaturalBotLanguage language = PickLanguage();
            return CreateProfile(language, rankTier);
        }

        public static NaturalChatLine CreateChatLine(string channel)
        {
            NaturalBotLanguage language = PickLanguage();
            NaturalBotProfile profile = CreateProfile(language, null);
            string text = PickChatText(language, channel);
            return new NaturalChatLine(profile, text);
        }

        private static NaturalBotProfile CreateProfile(NaturalBotLanguage language, string rankTier)
        {
            string nickname = BuildNickname(language, rankTier);
            string status = PickStatus(language);
            return new NaturalBotProfile(nickname, language, status);
        }

        private static NaturalBotLanguage PickLanguage()
        {
            float roll = Random.value;
            if (roll < 0.36f)
                return NaturalBotLanguage.Turkish;
            if (roll < 0.64f)
                return NaturalBotLanguage.Russian;
            if (roll < 0.82f)
                return NaturalBotLanguage.German;
            return NaturalBotLanguage.English;
        }

        private static string BuildNickname(NaturalBotLanguage language, string rankTier)
        {
            string realName = Pick(NamePool(language));
            string handle = Pick(Handles);
            int pattern = Random.Range(0, 9);
            bool highRank = string.Equals(rankTier, "Jade", System.StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(rankTier, "Master", System.StringComparison.OrdinalIgnoreCase);

            if (highRank && Random.value < 0.28f)
                pattern = 3;

            return pattern switch
            {
                0 => realName + Random.Range(17, 99),
                1 => realName + "_" + Random.Range(90, 100),
                2 => handle + Random.Range(7, 80),
                3 => handle,
                4 => realName + "." + Pick(new[] { "play", "gg", "rp", "mid", "one", "tr", "ru" }),
                5 => Pick(new[] { "mr", "ms", "old", "new", "real" }) + "_" + realName,
                6 => realName.ToLowerInvariant() + Random.Range(100, 999),
                7 => handle + "_" + Random.Range(2, 12),
                _ => realName
            };
        }

        private static string PickStatus(NaturalBotLanguage language)
        {
            return Pick(language switch
            {
                NaturalBotLanguage.Russian => StatusRussian,
                NaturalBotLanguage.English => StatusEnglish,
                NaturalBotLanguage.Turkish => StatusTurkish,
                NaturalBotLanguage.German => StatusGerman,
                _ => StatusEnglish
            });
        }

        private static string PickChatText(NaturalBotLanguage language, string channel)
        {
            bool mahjong = string.Equals(channel, GlobalChatService.ChannelMahjong, System.StringComparison.OrdinalIgnoreCase);
            return Pick((language, mahjong) switch
            {
                (NaturalBotLanguage.Russian, true) => MahjongRussianLines,
                (NaturalBotLanguage.English, true) => MahjongEnglishLines,
                (NaturalBotLanguage.Turkish, true) => MahjongTurkishLines,
                (NaturalBotLanguage.German, true) => MahjongGermanLines,
                (NaturalBotLanguage.Russian, false) => GlobalRussianLines,
                (NaturalBotLanguage.English, false) => GlobalEnglishLines,
                (NaturalBotLanguage.Turkish, false) => GlobalTurkishLines,
                (NaturalBotLanguage.German, false) => GlobalGermanLines,
                _ => GlobalEnglishLines
            });
        }

        private static string[] NamePool(NaturalBotLanguage language)
        {
            return language switch
            {
                NaturalBotLanguage.Russian => RussianNames,
                NaturalBotLanguage.English => EnglishNames,
                NaturalBotLanguage.Turkish => TurkishNames,
                NaturalBotLanguage.German => GermanNames,
                _ => EnglishNames
            };
        }

        private static string Pick(string[] values)
        {
            return values == null || values.Length == 0 ? string.Empty : values[Random.Range(0, values.Length)];
        }
    }
}
