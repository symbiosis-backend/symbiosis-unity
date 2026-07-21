using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dynasty.Legacy.CorrosionCollapse.UI
{
    public sealed class DiceUI : MonoBehaviour
    {
        [SerializeField] private Button diceButton;
        [SerializeField] private TextMeshProUGUI diceValues;
        [SerializeField] private Image turnTimerRing;

        public Button DiceButton => diceButton;

        public void Initialize(Button button, TextMeshProUGUI values, Image timerRing)
        {
            diceButton = button;
            diceValues = values;
            turnTimerRing = timerRing;
            ShowDiceResult(0, 0);
            SetInteractable(false);
            SetTurnTimer(1f);
        }

        public void SetInteractable(bool interactable)
        {
            if (diceButton != null)
            {
                diceButton.interactable = interactable;
            }
        }

        public void ShowDiceResult(int dice1, int dice2)
        {
            if (diceValues == null)
            {
                return;
            }

            diceValues.text = dice1 <= 0 || dice2 <= 0 ? "-- + --" : $"{dice1} + {dice2}";
        }

        public void SetTurnTimer(float normalized)
        {
            if (turnTimerRing != null)
            {
                turnTimerRing.fillAmount = Mathf.Clamp01(normalized);
            }
        }
    }
}
