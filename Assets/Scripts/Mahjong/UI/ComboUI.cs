using TMPro;
using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class ComboUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private string hiddenText = "";
        [SerializeField] private string comboPrefix = "COMBO x";

        private void Reset()
        {
            if (!comboText)
                comboText = GetComponent<TMP_Text>();
        }

        private void Update()
        {
            if (!comboText || ComboSystem.I == null)
                return;

            if (ComboSystem.I.IsComboActive)
                comboText.text = comboPrefix + ComboSystem.I.ComboLevel;
            else
                comboText.text = hiddenText;
        }
    }
}