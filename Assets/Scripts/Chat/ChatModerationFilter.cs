using System;
using System.Text;
using System.Text.RegularExpressions;

namespace MahjongGame
{
    public static class ChatModerationFilter
    {
        private static readonly string[] BlockedTerms =
        {
            "fuck", "fucker", "fucking", "fuk", "fck", "fuq", "stfu", "wtf",
            "shit", "sh1t", "crap", "bullshit", "bitch", "biatch", "b1tch",
            "cunt", "cnt", "dick", "dik", "cock", "pussy", "asshole", "arsehole",
            "bastard", "motherfucker", "mf", "slut", "whore", "hoe", "jerkoff",
            "retard", "retarded", "moron", "idiot", "kys",
            "nazi", "hitler", "racist",

            "blya", "blyat", "blin", "suka", "sucka", "sukablyat", "nahui",
            "nahuy", "naxui", "pizda", "pizdec", "pizdets", "pizduk", "pidor",
            "pidaras", "pidar", "huy", "hui", "xuy", "xyi", "huinya", "huilo",
            "ebat", "yebat", "ept", "yopt", "eblan", "mudak", "dolbaeb", "debil",
            "urod", "shluha", "kurwa", "kurva",
            "бля", "блять", "блин", "сука", "сучка", "нахуй", "нахер", "хер",
            "пизда", "пиздец", "пиздук", "пидор", "пидарас", "хуй", "хуи",
            "хуило", "хуйня", "ебать", "ёб", "ебан", "еблан", "мудила", "мудак",
            "долбоеб", "долбоёб", "дебил", "урод", "шлюха", "проститутка",

            "amk", "aq", "mk", "oc", "oç", "siktir", "sikerim", "sikik", "sik",
            "orospu", "kahpe", "pic", "piç", "yarrak", "yarak", "göt",
            "gotveren", "götveren", "pezevenk", "ibne", "salak", "aptal",

            "puta", "puto", "putain", "merde", "connard", "salope", "encule",
            "enculé", "batard", "bâtard", "scheisse", "scheiße", "arschloch",
            "fotze", "hurensohn", "verdammt", "stronzo", "cazzo", "merda",
            "puttana", "vaffanculo", "pendejo", "cabron", "cabrón", "mierda",
            "joder", "carajo", "coño", "filho da puta", "porra", "caralho",
            "foda", "buceta", "pierdole", "pierdolić", "chuj", "dupa", "jebac",
            "jebać", "suka", "kurwa", "cholera"
        };

        public static string Clean(string value, out bool changed)
        {
            changed = false;
            string normalized = string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : Regex.Replace(value.Trim(), @"\s+", " ");

            if (string.IsNullOrWhiteSpace(normalized))
                return string.Empty;

            string[] tokens = Regex.Split(normalized, @"(\s+)");
            StringBuilder result = new StringBuilder(normalized.Length);
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                if (string.IsNullOrEmpty(token) || Regex.IsMatch(token, @"^\s+$") || !ContainsBlockedLanguage(token))
                {
                    result.Append(token);
                    continue;
                }

                changed = true;
                result.Append(MaskToken(token));
            }

            if (!changed && ContainsBlockedLanguage(normalized))
            {
                changed = true;
                return "[message moderated]";
            }

            return result.ToString();
        }

        private static bool ContainsBlockedLanguage(string value)
        {
            string compact = Compact(value);
            if (string.IsNullOrEmpty(compact))
                return false;

            for (int i = 0; i < BlockedTerms.Length; i++)
            {
                string term = Compact(BlockedTerms[i]);
                if (string.IsNullOrEmpty(term))
                    continue;

                bool matched = term.Length <= 3
                    ? string.Equals(compact, term, StringComparison.Ordinal)
                    : compact.Contains(term);
                if (matched)
                    return true;
            }

            return false;
        }

        private static string Compact(string value)
        {
            string normalized = Normalize(value);
            StringBuilder result = new StringBuilder(normalized.Length);
            for (int i = 0; i < normalized.Length; i++)
            {
                char c = normalized[i];
                if ((c >= 'a' && c <= 'z') || (c >= 'а' && c <= 'я') || c == 'е')
                    result.Append(c);
            }

            return result.ToString();
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            StringBuilder result = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = char.ToLowerInvariant(value[i]);
                switch (c)
                {
                    case 'ё':
                        result.Append('е');
                        break;
                    case 'ı':
                    case 'i':
                    case '1':
                    case '!':
                    case '|':
                        result.Append('i');
                        break;
                    case 'ö':
                    case '0':
                        result.Append('o');
                        break;
                    case 'ü':
                        result.Append('u');
                        break;
                    case 'ş':
                    case '5':
                    case '$':
                        result.Append('s');
                        break;
                    case 'ğ':
                        result.Append('g');
                        break;
                    case 'ç':
                        result.Append('c');
                        break;
                    case '3':
                        result.Append('e');
                        break;
                    case '4':
                    case '@':
                        result.Append('a');
                        break;
                    case '7':
                        result.Append('t');
                        break;
                    default:
                        result.Append(c);
                        break;
                }
            }

            return result.ToString();
        }

        private static string MaskToken(string token)
        {
            StringBuilder result = new StringBuilder(token.Length);
            for (int i = 0; i < token.Length; i++)
            {
                char c = token[i];
                result.Append(char.IsLetterOrDigit(c) ? '*' : c);
            }

            return result.ToString();
        }
    }
}
