using TMPro;
using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class LobbyScoreUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text totalScoreText;
        [SerializeField] private string prefix = "Total Score: ";

        private void Reset()
        {
            if (!totalScoreText)
                totalScoreText = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (!totalScoreText)
                return;

            int total = PlayerPrefs.GetInt("Mahjong_TotalScore", 0);
            totalScoreText.text = prefix + total.ToString();
        }
    }
}