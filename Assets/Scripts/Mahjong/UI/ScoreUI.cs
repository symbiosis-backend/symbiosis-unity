using TMPro;
using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class ScoreUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private string prefix = "Score: ";

        private void Reset()
        {
            if (!scoreText)
                scoreText = GetComponent<TMP_Text>();
        }

        private void Update()
        {
            if (!scoreText || ScoreSystem.I == null)
                return;

            scoreText.text = prefix + ScoreSystem.I.CurrentLevelScore.ToString();
        }
    }
}