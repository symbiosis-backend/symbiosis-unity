using TMPro;
using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class CurrencyView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI altinText;
        [SerializeField] private TextMeshProUGUI ametistText;

        [Header("Format")]
        [SerializeField] private string altinFormat = "{0}";
        [SerializeField] private string ametistFormat = "{0}";

        private void OnEnable()
        {
            CurrencyService.CurrencyChanged += Refresh;
            ProfileService.ProfileChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            CurrencyService.CurrencyChanged -= Refresh;
            ProfileService.ProfileChanged -= Refresh;
        }

        public void Refresh()
        {
            if (CurrencyService.I == null)
                return;

            int altin = CurrencyService.I.GetOzAltin();
            int ametist = CurrencyService.I.GetOzAmetist();

            if (altinText != null)
                altinText.text = string.Format(altinFormat, altin);

            if (ametistText != null)
                ametistText.text = string.Format(ametistFormat, ametist);
        }
    }
}