using UnityEngine;
using TMPro;

namespace OkeyGame
{
    [DisallowMultipleComponent]
    public sealed class OkeyWinController : MonoBehaviour
    {
        [Header("Links")]
        public OkeyTurnManager TurnManager;

        [Header("UI")]
        public GameObject WinPanel;
        public TMP_Text WinText;

        [Header("Runtime (Read Only)")]
        [SerializeField] private bool gameEnded = false;
        [SerializeField] private int winnerSeatIndex = -1;
        [SerializeField] private bool isDraw = false;
        [SerializeField] private bool isCifteWin = false;

        public bool GameEnded => gameEnded;
        public int WinnerSeatIndex => winnerSeatIndex;
        public bool IsDraw => isDraw;
        public bool IsCifteWin => isCifteWin;

        private void Start()
        {
            HideWinUI();
        }

        public bool TryDeclareWin(OkeyPlayerSeat seat)
        {
            if (seat == null)
            {
                Debug.LogWarning("[OkeyWinController] Seat is NULL.");
                return false;
            }

            if (gameEnded)
            {
                Debug.Log("[OkeyWinController] Game already ended.");
                return false;
            }

            if (TurnManager == null)
            {
                Debug.LogWarning("[OkeyWinController] TurnManager is NULL.");
                return false;
            }

            if (TurnManager.Table == null)
            {
                Debug.LogWarning("[OkeyWinController] TurnManager.Table is NULL.");
                return false;
            }

            OkeyHandAnalyzer.WinCheckResult result = OkeyHandAnalyzer.CheckWinningHand(seat.Tiles, TurnManager.Table);
            if (result == null)
            {
                Debug.LogWarning("[OkeyWinController] Win check returned NULL.");
                return false;
            }

            if (!result.IsWinningHand)
            {
                Debug.Log($"[OkeyWinController] P{seat.SeatIndex} is not winning.");
                return false;
            }

            gameEnded = true;
            isDraw = false;
            isCifteWin = result.IsCifte;
            winnerSeatIndex = seat.SeatIndex;

            ShowWinUI(seat.SeatIndex, result.IsCifte);

            Debug.Log($"[OkeyWinController] WINNER = P{seat.SeatIndex} | CIFTE = {result.IsCifte}");

            if (!result.IsCifte)
            {
                for (int i = 0; i < result.WinningGroups.Count; i++)
                    Debug.Log($"[OkeyWinController] WIN GROUP {i + 1}: {result.WinningGroups[i]}");
            }
            else
            {
                Debug.Log("[OkeyWinController] WIN TYPE = CIFTE");
            }

            return true;
        }

        public bool DeclareDraw()
        {
            if (gameEnded)
            {
                Debug.Log("[OkeyWinController] Draw ignored. Game already ended.");
                return false;
            }

            gameEnded = true;
            isDraw = true;
            isCifteWin = false;
            winnerSeatIndex = -1;

            ShowDrawUI();

            Debug.Log("[OkeyWinController] ROUND ENDED IN DRAW.");
            return true;
        }

        public void ResetRound()
        {
            gameEnded = false;
            winnerSeatIndex = -1;
            isDraw = false;
            isCifteWin = false;
            HideWinUI();

            Debug.Log("[OkeyWinController] Round reset.");
        }

        private void ShowWinUI(int seatIndex, bool cifte)
        {
            if (WinPanel != null)
                WinPanel.SetActive(true);

            if (WinText != null)
                WinText.text = cifte
                    ? $"P{seatIndex} WINS - ÇİFTE"
                    : $"P{seatIndex} WINS";
        }

        private void ShowDrawUI()
        {
            if (WinPanel != null)
                WinPanel.SetActive(true);

            if (WinText != null)
                WinText.text = "DRAW";
        }

        private void HideWinUI()
        {
            if (WinPanel != null)
                WinPanel.SetActive(false);

            if (WinText != null)
                WinText.text = string.Empty;
        }
    }
}