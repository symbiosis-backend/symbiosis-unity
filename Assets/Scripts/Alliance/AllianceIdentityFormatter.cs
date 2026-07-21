namespace MahjongGame
{
    public static class AllianceIdentityFormatter
    {
        public static string FormatName(string displayName, string allianceTag)
        {
            string name = string.IsNullOrWhiteSpace(displayName) ? "Player" : displayName.Trim();
            string tag = NormalizeTag(allianceTag);
            if (string.IsNullOrWhiteSpace(tag) || name.StartsWith("["))
                return name;

            return "[" + tag + "] " + name;
        }

        public static string FormatOwnName(PlayerProfile profile, string fallbackName = "Player")
        {
            string name = profile != null && !string.IsNullOrWhiteSpace(profile.DisplayName)
                ? profile.DisplayName.Trim()
                : fallbackName;

            return FormatName(name, ResolveOwnTag(profile));
        }

        public static string ResolveOwnTag(PlayerProfile profile = null)
        {
            string profileTag = profile != null ? NormalizeTag(profile.AllianceTag) : string.Empty;
            if (!string.IsNullOrWhiteSpace(profileTag))
                return profileTag;

            AllianceSummary current = AllianceService.I != null ? AllianceService.I.Current : null;
            return current != null ? NormalizeTag(current.tag) : string.Empty;
        }

        public static string ResolveOwnName(PlayerProfile profile = null)
        {
            string profileName = profile != null && !string.IsNullOrWhiteSpace(profile.AllianceName)
                ? profile.AllianceName.Trim()
                : string.Empty;
            if (!string.IsNullOrWhiteSpace(profileName))
                return profileName;

            AllianceSummary current = AllianceService.I != null ? AllianceService.I.Current : null;
            return current != null && !string.IsNullOrWhiteSpace(current.name) ? current.name.Trim() : string.Empty;
        }

        public static string NormalizeTag(string allianceTag)
        {
            return string.IsNullOrWhiteSpace(allianceTag) ? string.Empty : allianceTag.Trim().ToUpperInvariant();
        }
    }
}
