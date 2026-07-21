using UnityEngine;

namespace MahjongGame.Monetization
{
    public static class MonetizationAdSettings
    {
        public const int RewardedAdMaxAgeSeconds = 60 * 60;
        public const int InterstitialAdMaxAgeSeconds = 60 * 60;

        private const string MatchEndShowEveryKey = "monetization_match_end_show_every";
        private const string MatchEndCooldownKey = "monetization_match_end_cooldown_seconds";
        private const int DefaultMatchEndShowEvery = 1;
        private const int DefaultMatchEndCooldownSeconds = 30;

        public static int MatchEndShowEveryCount =>
            Mathf.Clamp(PlayerPrefs.GetInt(MatchEndShowEveryKey, DefaultMatchEndShowEvery), 1, 20);

        public static int MatchEndCooldownSeconds =>
            Mathf.Clamp(PlayerPrefs.GetInt(MatchEndCooldownKey, DefaultMatchEndCooldownSeconds), 0, 3600);

        public static void SetMatchEndFrequencyForTesting(int showEveryMatchCount, int cooldownSeconds)
        {
            PlayerPrefs.SetInt(MatchEndShowEveryKey, Mathf.Clamp(showEveryMatchCount, 1, 20));
            PlayerPrefs.SetInt(MatchEndCooldownKey, Mathf.Clamp(cooldownSeconds, 0, 3600));
            PlayerPrefs.Save();
        }

        public static void ResetMatchEndFrequencyTestingOverrides()
        {
            PlayerPrefs.DeleteKey(MatchEndShowEveryKey);
            PlayerPrefs.DeleteKey(MatchEndCooldownKey);
            PlayerPrefs.Save();
        }
    }
}
