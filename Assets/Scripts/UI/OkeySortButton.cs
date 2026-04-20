using UnityEngine;

namespace OkeyGame
{
    [DisallowMultipleComponent]
    public sealed class OkeySortButton : MonoBehaviour
    {
        [Header("Links")]
        public OkeyTurnManager TurnManager;
        public OkeyPlayerSeat TargetSeat;

        public void OnClickSort()
        {
            if (TurnManager != null && TurnManager.WinController != null && TurnManager.WinController.GameEnded)
            {
                Debug.Log("[OkeySortButton] Sort blocked. Game already ended.");
                return;
            }

            OkeyPlayerSeat seat = TargetSeat;

            if (seat == null && TurnManager != null)
                seat = TurnManager.LocalSeat;

            if (seat == null)
            {
                Debug.LogWarning("[OkeySortButton] TargetSeat is NULL.");
                return;
            }

            seat.ToggleSortModeAndSort();
            Debug.Log($"[OkeySortButton] Sort pressed for P{seat.SeatIndex}");
        }
    }
}