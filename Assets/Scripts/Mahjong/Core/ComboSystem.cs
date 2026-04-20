using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class ComboSystem : MonoBehaviour
    {
        public static ComboSystem I { get; private set; }

        [Header("Combo Rules")]
        [SerializeField, Min(1)] private int comboStartsAtStreak = 5;
        [SerializeField, Min(0)] private int comboBonusStep = 25;

        public int PairStreak { get; private set; }
        public int ComboLevel => Mathf.Max(0, PairStreak - (comboStartsAtStreak - 1));
        public int ComboBonusStep => comboBonusStep;
        public bool IsComboActive => ComboLevel > 0;

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
        }

        public void RegisterSuccessPair()
        {
            PairStreak++;
        }

        public void ResetCombo()
        {
            PairStreak = 0;
        }

        public int GetCurrentBonus()
        {
            return ComboLevel * comboBonusStep;
        }
    }
}