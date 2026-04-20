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

        [Header("Fallback")]
        [SerializeField] private string fallbackName = "Player";
        [SerializeField] private string fallbackRankTier = "Unranked";

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
            bool isBattle = MahjongSession.LaunchMode == MahjongLaunchMode.Battle;

            if (battleHudRoot != null)
                battleHudRoot.SetActive(isBattle);

            if (!isBattle)
                return;

            string opponentName = string.IsNullOrWhiteSpace(MahjongSession.BattleOpponentName)
                ? fallbackName
                : MahjongSession.BattleOpponentName;

            string rankTier = string.IsNullOrWhiteSpace(MahjongSession.BattleOpponentRankTier)
                ? fallbackRankTier
                : MahjongSession.BattleOpponentRankTier;

            int rankPoints = Mathf.Max(0, MahjongSession.BattleOpponentRankPoints);
            int avatarId = Mathf.Max(0, MahjongSession.BattleOpponentAvatarId);

            if (opponentNameText != null)
                opponentNameText.text = opponentName;

            if (opponentRankTierText != null)
                opponentRankTierText.text = rankTier;

            if (opponentRankPointsText != null)
                opponentRankPointsText.text = rankPointsPrefix + rankPoints;

            ApplyAvatar(avatarId);
        }

        private void ApplyAvatar(int avatarId)
        {
            if (opponentAvatarImage == null)
                return;

            Sprite chosen = defaultAvatarSprite;

            if (avatarSprites != null && avatarSprites.Length > 0)
            {
                if (avatarId >= 0 && avatarId < avatarSprites.Length && avatarSprites[avatarId] != null)
                    chosen = avatarSprites[avatarId];
                else if (defaultAvatarSprite == null)
                    chosen = avatarSprites[0];
            }

            opponentAvatarImage.sprite = chosen;
            opponentAvatarImage.enabled = chosen != null;
        }
    }
}