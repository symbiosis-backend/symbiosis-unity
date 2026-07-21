using System.Collections.Generic;
using Dynasty.Legacy.CorrosionCollapse.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dynasty.Legacy.CorrosionCollapse.UI
{
    public sealed class ResultUI : MonoBehaviour
    {
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI winnerText;
        [SerializeField] private TextMeshProUGUI scoreSummary;
        [SerializeField] private Button restartButton;

        public void Initialize(GameObject panel, TextMeshProUGUI winner, TextMeshProUGUI summary, Button restart)
        {
            resultPanel = panel;
            winnerText = winner;
            scoreSummary = summary;
            restartButton = restart;
            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }
        }

        public void ShowResult(string title, IReadOnlyList<PlayerState> players = null)
        {
            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
            }

            if (winnerText != null)
            {
                winnerText.text = title;
            }

            if (scoreSummary == null)
            {
                return;
            }

            if (players == null)
            {
                scoreSummary.text = string.Empty;
                return;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            foreach (PlayerState player in players)
            {
                string status = player.finished ? "finished" : player.eliminated ? "eliminated" : "alive";
                builder.AppendLine($"{player.nickname}  {player.score}  {status}");
            }

            scoreSummary.text = builder.ToString();
        }
    }
}
