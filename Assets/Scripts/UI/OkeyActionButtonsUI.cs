using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OkeyGame
{
    [DisallowMultipleComponent]
    public sealed class OkeyActionButtonsUI : MonoBehaviour
    {
        [Header("Links")]
        public OkeyTurnManager TurnManager;
        public OkeyPlayerSeat LocalSeat;

        [Header("Buttons")]
        public Button BtnCifteGit;
        public Button BtnCiftDiz;

        [Header("Optional UI")]
        public TMP_Text StateText;

        [Header("Runtime")]
        [SerializeField] private bool isCifteGitActive;

        private bool lastCifteInteractable;
        private bool lastCiftDizInteractable;
        private string lastStateText = "";

        public bool IsCifteGitActive => isCifteGitActive;

        private void Awake()
        {
            AutoResolve();
            BindButtons();
            RefreshUI(true);
        }

        private void OnEnable()
        {
            AutoResolve();
            BindButtons();
            RefreshUI(true);
        }

        private void Update()
        {
            RefreshRuntimeState();
            RefreshUI(false);
        }

        private void AutoResolve()
        {
            if (TurnManager == null)
                TurnManager = FindAnyObjectByType<OkeyTurnManager>();

            if (LocalSeat == null && TurnManager != null)
                LocalSeat = TurnManager.LocalSeat;

            if (LocalSeat == null)
                LocalSeat = FindLocalSeatFallback();
        }

        private OkeyPlayerSeat FindLocalSeatFallback()
        {
            OkeyPlayerSeat[] allSeats = FindObjectsByType<OkeyPlayerSeat>(FindObjectsInactive.Exclude);
            if (allSeats == null || allSeats.Length == 0)
                return null;

            if (TurnManager != null)
            {
                for (int i = 0; i < allSeats.Length; i++)
                {
                    if (allSeats[i] != null && allSeats[i].SeatIndex == TurnManager.LocalSeatIndex)
                        return allSeats[i];
                }
            }

            return allSeats[0];
        }

        private void BindButtons()
        {
            if (BtnCifteGit != null)
            {
                BtnCifteGit.onClick.RemoveListener(OnClickCifteGit);
                BtnCifteGit.onClick.AddListener(OnClickCifteGit);
            }

            if (BtnCiftDiz != null)
            {
                BtnCiftDiz.onClick.RemoveListener(OnClickCiftDiz);
                BtnCiftDiz.onClick.AddListener(OnClickCiftDiz);
            }
        }

        public void OnClickCifteGit()
        {
            AutoResolve();

            if (TurnManager == null)
                return;

            bool ok = TurnManager.TryDeclareCifteForLocalPlayer();
            if (!ok)
                return;

            isCifteGitActive = true;
            SetStateText("ÇİFTE GİT AKTİF");
            RefreshUI(true);
        }

        public void OnClickCiftDiz()
        {
            AutoResolve();

            if (LocalSeat == null || TurnManager == null || TurnManager.Table == null)
                return;

            LocalSeat.SortHandAsPairs(TurnManager.Table);
            SetStateText("ÇİFT DİZ UYGULANDI");
            RefreshUI(true);
        }

        private void RefreshRuntimeState()
        {
            if (TurnManager == null)
            {
                isCifteGitActive = false;
                return;
            }

            if (TurnManager.IsGameEnded())
            {
                isCifteGitActive = false;
                return;
            }
        }

        private void RefreshUI(bool force)
        {
            AutoResolve();

            bool canUse = TurnManager != null && !TurnManager.IsGameEnded();
            bool cifteInteractable = canUse && TurnManager != null && TurnManager.CanLocalPlayerDeclareCifte();
            bool ciftDizInteractable = canUse && LocalSeat != null && LocalSeat.HandCount > 0;

            if (BtnCifteGit != null && (force || lastCifteInteractable != cifteInteractable))
            {
                BtnCifteGit.interactable = cifteInteractable;
                lastCifteInteractable = cifteInteractable;
            }

            if (BtnCiftDiz != null && (force || lastCiftDizInteractable != ciftDizInteractable))
            {
                BtnCiftDiz.interactable = ciftDizInteractable;
                lastCiftDizInteractable = ciftDizInteractable;
            }

            if (StateText != null && force && string.IsNullOrWhiteSpace(StateText.text) && !string.IsNullOrWhiteSpace(lastStateText))
                StateText.text = lastStateText;
        }

        private void SetStateText(string text)
        {
            lastStateText = text;

            if (StateText != null)
                StateText.text = text;
        }

        public void ResetCifteGitState()
        {
            isCifteGitActive = false;
            RefreshUI(true);
        }
    }
}