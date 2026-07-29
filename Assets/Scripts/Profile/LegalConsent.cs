using UnityEngine;

namespace MahjongGame
{
    public static class LegalConsent
    {
        public const string CurrentVersion = "2026-07-29";
        public const string TermsUrl = "https://dlsymbiosis.com/terms";
        public const string PrivacyUrl = "https://dlsymbiosis.com/privacy";

        private const string AcceptedVersionKey = "dlsymbiosis_legal_consent_version";
        private const string AcceptedAtKey = "dlsymbiosis_legal_consent_accepted_at";

        public static bool HasAcceptedCurrentVersion =>
            string.Equals(PlayerPrefs.GetString(AcceptedVersionKey, string.Empty), CurrentVersion);

        public static void AcceptCurrentVersion()
        {
            PlayerPrefs.SetString(AcceptedVersionKey, CurrentVersion);
            PlayerPrefs.SetString(AcceptedAtKey, System.DateTime.UtcNow.ToString("O"));
            PlayerPrefs.Save();
        }

        public static string ConsentLabel()
        {
            switch (CurrentLanguage())
            {
                case GameLanguage.Russian:
                    return "Я принимаю Условия, Политику конфиденциальности и правила сообщества";
                case GameLanguage.Turkish:
                    return "Kullanım Koşulları, Gizlilik Politikası ve Topluluk Kurallarını kabul ediyorum";
                case GameLanguage.German:
                    return "Ich akzeptiere Nutzungsbedingungen, Datenschutz und Community-Regeln";
                default:
                    return "I accept the Terms, Privacy Policy, and Community Guidelines";
            }
        }

        public static string ViewTermsLabel()
        {
            switch (CurrentLanguage())
            {
                case GameLanguage.Russian:
                    return "Правила";
                case GameLanguage.Turkish:
                    return "Kurallar";
                case GameLanguage.German:
                    return "Regeln";
                default:
                    return "View Terms";
            }
        }

        public static string ConsentRequiredError()
        {
            switch (CurrentLanguage())
            {
                case GameLanguage.Russian:
                    return "Перед входом или регистрацией примите Условия и правила сообщества.";
                case GameLanguage.Turkish:
                    return "Giriş veya kayıt öncesinde Koşulları ve Topluluk Kurallarını kabul edin.";
                case GameLanguage.German:
                    return "Bitte akzeptiere vor Anmeldung oder Registrierung die Bedingungen und Community-Regeln.";
                default:
                    return "Accept the Terms and Community Guidelines before signing in or registering.";
            }
        }

        private static GameLanguage CurrentLanguage()
        {
            return AppSettings.I != null ? AppSettings.I.Language : GameLanguage.English;
        }
    }
}
