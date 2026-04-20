using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OkeyGame
{
    [DisallowMultipleComponent]
    public sealed class OkeyRoundFlow : MonoBehaviour
    {
        public enum RoundStage
        {
            Idle = 0,
            Dealing = 1,
            WaitingInitialDiscard = 2,
            Playing = 3,
            Ended = 4
        }

        [Header("Links")]
        public OkeyTableModule Table;
        public OkeyDealer Dealer;
        public OkeyTurnManager TurnManager;
        public Canvas RootCanvas;
        public RectTransform DrawPileRoot;
        public List<OkeyPlayerSeat> Seats = new();

        [Header("Rules")]
        [Range(0, 3)] public int StartPlayerIndex = 0;
        [Min(1)] public int BaseHandCount = 14;

        [Header("Visual Deal")]
        public bool AutoStartOnPlay = true;
        public bool AnimateDeal = true;
        [Min(0.01f)] public float DealDuration = 0.12f;
        [Min(0f)] public float DelayBetweenTiles = 0.03f;

        [Header("Runtime")]
        [SerializeField] private RoundStage stage = RoundStage.Idle;
        [SerializeField] private bool busy;

        public RoundStage Stage => stage;
        public bool IsBusy => busy;

        private readonly OkeyPlayerSeat[] seatMap = new OkeyPlayerSeat[4];
        private Coroutine roundRoutine;

        private void Awake()
        {
            if (RootCanvas == null)
                RootCanvas = FindAnyObjectByType<Canvas>();

            ResolveDrawPileRoot();
            NormalizeLinks();
        }

        private void Start()
        {
            if (AutoStartOnPlay)
                StartPreparedRound();
        }

        private void OnDisable()
        {
            if (roundRoutine != null)
            {
                StopCoroutine(roundRoutine);
                roundRoutine = null;
            }

            busy = false;
        }

        private void Update()
        {
            if (TurnManager == null)
                return;

            if (stage == RoundStage.WaitingInitialDiscard)
            {
                // Пока стартовый игрок не скинет первый лишний камень, ждём.
                // Как только ход ушёл дальше или фаза стала MustDraw - переходим в Playing.
                if (TurnManager.CurrentSeatIndex != StartPlayerIndex ||
                    TurnManager.CurrentPhase == OkeyTurnManager.TurnPhase.MustDraw)
                {
                    stage = RoundStage.Playing;
                }
            }
        }

        [ContextMenu("Start Prepared Round")]
        public void StartPreparedRound()
        {
            if (busy)
                return;

            if (roundRoutine != null)
                StopCoroutine(roundRoutine);

            roundRoutine = StartCoroutine(CoStartPreparedRound());
        }

        private IEnumerator CoStartPreparedRound()
        {
            busy = true;
            stage = RoundStage.Dealing;

            NormalizeLinks();

            if (Dealer == null || TurnManager == null)
            {
                Debug.LogError("[OkeyRoundFlow] Dealer or TurnManager is NULL.");
                busy = false;
                stage = RoundStage.Idle;
                roundRoutine = null;
                yield break;
            }

            // ВАЖНО:
            // Dealer теперь сам полностью подготавливает раунд:
            // - чистит стол
            // - строит колоду
            // - раздаёт 15/14/14/14
            // - вызывает InitializeRoundAfterDeal(StartPlayerIndex)
            Dealer.StartPlayerIndex = StartPlayerIndex;
            Dealer.PrepareNewRound();

            RefreshPileView();

            // Небольшая пауза на кадр, чтобы UI успел обновиться.
            yield return null;

            // После корректной подготовки стартовый игрок должен иметь фазу MustDiscard.
            if (TurnManager.CurrentSeatIndex == StartPlayerIndex &&
                TurnManager.CurrentPhase == OkeyTurnManager.TurnPhase.MustDiscard)
            {
                stage = RoundStage.WaitingInitialDiscard;
            }
            else
            {
                // Защита на случай рассинхрона.
                stage = RoundStage.Playing;
            }

            busy = false;
            roundRoutine = null;
        }

        private void NormalizeLinks()
        {
            for (int i = 0; i < seatMap.Length; i++)
                seatMap[i] = null;

            if ((Seats == null || Seats.Count == 0) && TurnManager != null && TurnManager.Seats != null)
                Seats = new List<OkeyPlayerSeat>(TurnManager.Seats);

            for (int i = 0; i < Seats.Count; i++)
            {
                if (Seats[i] == null)
                    continue;

                int idx = Seats[i].SeatIndex;
                if (idx < 0 || idx > 3)
                    continue;

                seatMap[idx] = Seats[i];
            }

            if (TurnManager != null)
                TurnManager.LocalSeat = GetSeatByIndex(TurnManager.LocalSeatIndex);

            ResolveDrawPileRoot();
        }

        private void ResolveDrawPileRoot()
        {
            if (DrawPileRoot != null)
                return;

            var pile = FindAnyObjectByType<OkeyDrawPile>();
            if (pile != null)
                DrawPileRoot = pile.DrawPileRoot as RectTransform;
        }

        private OkeyPlayerSeat GetSeatByIndex(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex > 3)
                return null;

            return seatMap[seatIndex];
        }

        private void RefreshPileView()
        {
            if (Dealer != null && Dealer.DrawPileView != null)
            {
                Dealer.DrawPileView.RefreshView();
                return;
            }

            var pile = FindAnyObjectByType<OkeyDrawPile>();
            if (pile != null)
                pile.RefreshView();
        }
    }
}