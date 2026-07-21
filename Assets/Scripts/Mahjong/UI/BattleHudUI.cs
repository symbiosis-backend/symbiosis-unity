using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class BattleHudUI : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject battleHudRoot;

        [Header("Texts")]
        [SerializeField] private TMP_Text opponentNameText;
        [SerializeField] private TMP_Text opponentRankTierText;
        [SerializeField] private TMP_Text opponentRankPointsText;

        [Header("Avatar")]
        [SerializeField] private Image opponentAvatarImage;
        [SerializeField] private Sprite defaultAvatarSprite;
        [SerializeField] private Sprite[] avatarSprites;

        [Header("Labels")]
        [SerializeField] private string rankPointsPrefix = "RP: ";

        private void Awake()
        {
            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            ApplyBattleFont();

            bool isBattle = MahjongSession.LaunchMode == MahjongLaunchMode.Battle;

            if (battleHudRoot != null)
                battleHudRoot.SetActive(isBattle);

            if (!isBattle)
                return;

            string opponentName = string.IsNullOrWhiteSpace(MahjongSession.BattleOpponentName)
                ? GameLocalization.Text("battle.common.player")
                : MahjongSession.BattleOpponentName;
            opponentName = AllianceIdentityFormatter.FormatName(opponentName, MahjongSession.BattleOpponentAllianceTag);

            string rankTier = string.IsNullOrWhiteSpace(MahjongSession.BattleOpponentRankTier)
                ? GameLocalization.Text("battle.rank.unranked")
                : LocalizeRankTier(MahjongSession.BattleOpponentRankTier);

            int rankPoints = Mathf.Max(0, MahjongSession.BattleOpponentRankPoints);
            int avatarId = Mathf.Max(0, MahjongSession.BattleOpponentAvatarId);

            if (opponentNameText != null)
                opponentNameText.text = opponentName;

            if (opponentRankTierText != null)
                opponentRankTierText.text = rankTier;

            if (opponentRankPointsText != null)
                opponentRankPointsText.text = rankPointsPrefix + rankPoints;

            ApplyAvatar(avatarId, MahjongSession.BattleOpponentGender);
        }

        private void ApplyAvatar(int avatarId, PlayerGender gender)
        {
            if (opponentAvatarImage == null)
                return;

            Sprite chosen = defaultAvatarSprite;

            if (gender == PlayerGender.Male || gender == PlayerGender.Female)
            {
                Sprite resourceAvatar = ProfileAvatarResources.GetSprite(gender, avatarId);
                if (resourceAvatar != null)
                    chosen = resourceAvatar;
            }

            if (chosen == defaultAvatarSprite && avatarSprites != null && avatarSprites.Length > 0)
            {
                if (avatarId >= 0 && avatarId < avatarSprites.Length && avatarSprites[avatarId] != null)
                    chosen = avatarSprites[avatarId];
                else if (defaultAvatarSprite == null)
                    chosen = avatarSprites[0];
            }

            opponentAvatarImage.sprite = chosen;
            opponentAvatarImage.enabled = chosen != null;
        }

        private void ApplyBattleFont()
        {
            BattlePopupStyle.ApplyFontOnly(opponentNameText);
            BattlePopupStyle.ApplyFontOnly(opponentRankTierText);
            BattlePopupStyle.ApplyFontOnly(opponentRankPointsText);
        }

        private static string LocalizeRankTier(string tier)
        {
            if (string.IsNullOrWhiteSpace(tier))
                return GameLocalization.Text("battle.rank.unranked");

            string value = tier.Trim().ToLowerInvariant();
            if (value.Contains("master")) return GameLocalization.Text("battle.rank.master");
            if (value.Contains("platinum")) return GameLocalization.Text("battle.rank.platinum");
            if (value.Contains("gold")) return GameLocalization.Text("battle.rank.gold");
            if (value.Contains("silver")) return GameLocalization.Text("battle.rank.silver");
            if (value.Contains("bronze")) return GameLocalization.Text("battle.rank.bronze");
            if (value.Contains("unranked")) return GameLocalization.Text("battle.rank.unranked");
            return tier.Trim();
        }
    }
}
