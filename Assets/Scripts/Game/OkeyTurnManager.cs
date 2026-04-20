using System.Collections.Generic;
using UnityEngine;

namespace OkeyGame
{
    public sealed class OkeyTurnManager : MonoBehaviour
    {
        public enum TurnPhase
        {
            MustDraw = 0,
            MustDiscard = 1
        }

        public enum PendingDrawSource
        {
            None = 0,
            Deck = 1,
            Discard = 2
        }

        [Header("Core Links")]
        public OkeyTableModule Table;
        public OkeyPlayerSeat LocalSeat;
        public List<OkeyPlayerSeat> Seats = new();
        public OkeyWinController WinController;

        [Header("Local Settings")]
        [Range(0, 3)] public int LocalSeatIndex = 0;

        [Header("Runtime (Read Only)")]
        [SerializeField] private int currentSeatIndex = 0;
        [SerializeField] private TurnPhase currentPhase = TurnPhase.MustDraw;
        [SerializeField] private PendingDrawSource pendingDrawSource = PendingDrawSource.None;
        [SerializeField] private OkeyTileInstance pendingDrawTile;
        [SerializeField] private OkeyDiscardPile pendingDiscardPile;
        [SerializeField] private bool drawTakenThisTurn = false;

        [Header("Çifte State (Read Only)")]
        [SerializeField] private bool[] seatDeclaredCifte = new bool[4];

        public int CurrentSeatIndex => currentSeatIndex;
        public TurnPhase CurrentPhase => currentPhase;
        public PendingDrawSource CurrentPendingDrawSource => pendingDrawSource;
        public OkeyTileInstance PendingDrawTile => pendingDrawTile;
        public bool HasLocalDeclaredCifte => HasSeatDeclaredCifte(LocalSeatIndex);

        private readonly OkeyPlayerSeat[] seatByIndex = new OkeyPlayerSeat[4];

        private void Awake()
        {
            CacheSeats();
            ClearAllCifteDeclarations();
        }

        private void CacheSeats()
        {
            for (int i = 0; i < seatByIndex.Length; i++)
                seatByIndex[i] = null;

            for (int i = 0; i < Seats.Count; i++)
            {
                if (Seats[i] == null)
                    continue;

                int idx = Seats[i].SeatIndex;
                if (idx < 0 || idx > 3)
                    continue;

                seatByIndex[idx] = Seats[i];
            }

            LocalSeat = GetSeat(LocalSeatIndex);
        }

        public void InitializeRoundAfterDeal(int startSeatIndex)
        {
            CacheSeats();
            currentSeatIndex = Mathf.Clamp(startSeatIndex, 0, 3);
            ClearPendingDrawState();
            ClearAllCifteDeclarations();
            drawTakenThisTurn = false;

            OkeyPlayerSeat startSeat = GetSeat(currentSeatIndex);
            currentPhase = (startSeat != null && startSeat.HandCount >= 15)
                ? TurnPhase.MustDiscard
                : TurnPhase.MustDraw;

            StartTurnForCurrentSeat();
        }

        public bool IsLocalPlayersTurn()
        {
            return currentSeatIndex == LocalSeatIndex;
        }

        public bool HasPendingLocalDraw()
        {
            return pendingDrawSource != PendingDrawSource.None || pendingDrawTile != null;
        }

        public bool IsGameEnded()
        {
            return IsGameLocked();
        }

        public bool HasSeatDeclaredCifte(int seatIndex)
        {
            return seatIndex >= 0 && seatIndex < seatDeclaredCifte.Length && seatDeclaredCifte[seatIndex];
        }

        public bool CanLocalPlayerDeclareCifte()
        {
            RefreshSeatCacheIfNeeded();
            SyncTurnStateWithSeat(LocalSeatIndex);

            if (IsGameLocked())
                return false;
            if (!IsLocalPlayersTurn())
                return false;
            if (LocalSeat == null)
                return false;
            if (LocalSeat.HandCount <= 0)
                return false;
            if (HasPendingLocalDraw())
                return false;
            if (HasSeatDeclaredCifte(LocalSeatIndex))
                return false;

            return true;
        }

        public bool TryDeclareCifteForLocalPlayer()
        {
            if (!CanLocalPlayerDeclareCifte())
                return false;

            seatDeclaredCifte[LocalSeatIndex] = true;
            Debug.Log($"[OkeyTurnManager] P{LocalSeatIndex} declared ÇIFTE.");
            return true;
        }

        public void ClearCifteDeclaration(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex >= seatDeclaredCifte.Length)
                return;

            seatDeclaredCifte[seatIndex] = false;
        }

        private void ClearAllCifteDeclarations()
        {
            if (seatDeclaredCifte == null || seatDeclaredCifte.Length != 4)
                seatDeclaredCifte = new bool[4];

            for (int i = 0; i < seatDeclaredCifte.Length; i++)
                seatDeclaredCifte[i] = false;
        }

        private bool IsGameLocked()
        {
            return WinController != null && WinController.GameEnded;
        }

        private bool CanSeatDraw(OkeyPlayerSeat seat)
        {
            if (seat == null)
                return false;

            if (!seat.CanAcceptOneMoreTile())
                return false;

            if (seat.HandCount >= 15)
                return false;

            return true;
        }

        private void RefreshSeatCacheIfNeeded()
        {
            if (LocalSeat == null || seatByIndex[LocalSeatIndex] == null)
                CacheSeats();
        }

        private void SyncTurnStateWithSeat(int seatIndex)
        {
            OkeyPlayerSeat seat = GetSeat(seatIndex);
            if (seat == null)
                return;

            if (seatIndex == LocalSeatIndex &&
                pendingDrawTile != null &&
                seat.ContainsTile(pendingDrawTile) &&
                seat.HandCount >= 15)
            {
                drawTakenThisTurn = true;
                currentPhase = TurnPhase.MustDiscard;
                ClearPendingDrawState();
                return;
            }

            currentPhase = seat.HandCount >= 15
                ? TurnPhase.MustDiscard
                : TurnPhase.MustDraw;
        }

        private void ApplyTableVisual(OkeyTileInstance tile)
        {
            if (tile == null)
                return;

            if (Table != null)
                Table.ApplyHandOrBoardVisual(tile);
            else
            {
                tile.ShowFront();
                tile.RefreshVisuals();
            }
        }

        private bool DeclareDrawIfDeckEmpty()
        {
            if (WinController != null)
                return WinController.DeclareDraw();

            Debug.LogWarning("[OkeyTurnManager] Deck ended, but WinController is NULL. Draw could not be shown.");
            return false;
        }

        public bool CanLocalPlayerTakeOneTile()
        {
            RefreshSeatCacheIfNeeded();
            SyncTurnStateWithSeat(LocalSeatIndex);

            if (IsGameLocked())
                return false;
            if (!IsLocalPlayersTurn())
                return false;
            if (currentPhase != TurnPhase.MustDraw)
                return false;
            if (HasPendingLocalDraw())
                return false;
            if (drawTakenThisTurn)
                return false;
            if (!CanSeatDraw(LocalSeat))
                return false;

            return true;
        }

        public bool CanStartLocalDrawFromDeck()
        {
            return CanLocalPlayerTakeOneTile();
        }

        public bool CanStartLocalDrawFromDiscard(OkeyDiscardPile pile)
        {
            RefreshSeatCacheIfNeeded();
            SyncTurnStateWithSeat(LocalSeatIndex);

            if (!CanLocalPlayerTakeOneTile())
                return false;
            if (pile == null || !pile.HasTile)
                return false;

            OkeyDiscardPile previousPile = GetPreviousSeatDiscardPile(LocalSeatIndex);
            return previousPile == pile;
        }

        public OkeyTileInstance BeginLocalDrawFromDeck()
        {
            RefreshSeatCacheIfNeeded();
            SyncTurnStateWithSeat(LocalSeatIndex);

            if (!CanStartLocalDrawFromDeck() || Table == null)
                return null;

            OkeyTileInstance tile = Table.DrawFromTop();
            if (tile == null)
            {
                DeclareDrawIfDeckEmpty();
                return null;
            }

            pendingDrawSource = PendingDrawSource.Deck;
            pendingDrawTile = tile;
            pendingDiscardPile = null;

            tile.gameObject.SetActive(true);
            tile.ShowBack();
            tile.RefreshVisuals();

            return tile;
        }

        public OkeyTileInstance BeginLocalDrawFromDiscard(OkeyDiscardPile pile)
        {
            RefreshSeatCacheIfNeeded();
            SyncTurnStateWithSeat(LocalSeatIndex);

            if (!CanStartLocalDrawFromDiscard(pile))
                return null;

            OkeyTileInstance tile = pile.TakeTopTile();
            if (tile == null)
                return null;

            pendingDrawSource = PendingDrawSource.Discard;
            pendingDrawTile = tile;
            pendingDiscardPile = pile;

            tile.gameObject.SetActive(true);
            ApplyTableVisual(tile);

            return tile;
        }

        public bool ConfirmLocalDrawPlacedIntoHand()
        {
            RefreshSeatCacheIfNeeded();
            SyncTurnStateWithSeat(LocalSeatIndex);

            if (IsGameLocked())
                return false;
            if (!IsLocalPlayersTurn())
                return false;
            if (pendingDrawTile == null)
                return false;
            if (LocalSeat == null)
                return false;
            if (!LocalSeat.ContainsTile(pendingDrawTile))
                return false;
            if (LocalSeat.HandCount != 15)
                return false;

            drawTakenThisTurn = true;
            currentPhase = TurnPhase.MustDiscard;
            ClearPendingDrawState();
            return true;
        }

        public void CancelPendingLocalDraw()
        {
            if (pendingDrawTile == null)
            {
                ClearPendingDrawState();
                return;
            }

            OkeyTileInstance tile = pendingDrawTile;
            PendingDrawSource source = pendingDrawSource;
            OkeyDiscardPile discardPile = pendingDiscardPile;

            if (LocalSeat != null && LocalSeat.ContainsTile(tile))
                LocalSeat.RemoveFromHand(tile);

            if (source == PendingDrawSource.Deck)
            {
                Table?.ReturnToBottom(tile);

                OkeyDrawPile drawPile = FindAnyObjectByType<OkeyDrawPile>();
                if (drawPile != null && drawPile.DrawPileRoot != null)
                {
                    tile.transform.SetParent(drawPile.DrawPileRoot, false);
                    tile.transform.SetAsLastSibling();
                }

                tile.gameObject.SetActive(true);
                tile.ShowBack();
                tile.RefreshVisuals();

                if (drawPile != null)
                    drawPile.RefreshView();

                OkeyIndicatorView indicatorView = FindAnyObjectByType<OkeyIndicatorView>();
                if (indicatorView != null && Table != null && Table.HasIndicator && Table.IndicatorTile != null)
                    indicatorView.ShowIndicator(Table.IndicatorTile);
            }
            else if (source == PendingDrawSource.Discard && discardPile != null)
            {
                discardPile.RestoreTopTile(tile);
                tile.gameObject.SetActive(true);
                ApplyTableVisual(tile);
            }
            else
            {
                tile.gameObject.SetActive(false);
            }

            ClearPendingDrawState();
            SyncTurnStateWithSeat(LocalSeatIndex);
        }

        public bool TryDrawForLocalPlayer()
        {
            RefreshSeatCacheIfNeeded();
            SyncTurnStateWithSeat(LocalSeatIndex);

            if (!CanLocalPlayerTakeOneTile())
            {
                Debug.LogWarning("[OkeyTurnManager] TryDrawForLocalPlayer rejected.");
                return false;
            }

            if (Table == null || LocalSeat == null)
                return false;

            int before = LocalSeat.HandCount;
            OkeyTileInstance tile = Table.DrawFromTop();
            if (tile == null)
            {
                DeclareDrawIfDeckEmpty();
                return false;
            }

            ApplyTableVisual(tile);
            LocalSeat.AddToHand(tile);

            if (LocalSeat.HandCount != before + 1 || LocalSeat.HandCount != 15)
            {
                LocalSeat.RemoveFromHand(tile);
                Table.ReturnToBottom(tile);
                return false;
            }

            drawTakenThisTurn = true;
            currentPhase = TurnPhase.MustDiscard;
            return true;
        }

        public bool TryDrawForSeat(int seatIndex)
        {
            SyncTurnStateWithSeat(seatIndex);

            if (IsGameLocked())
                return false;
            if (Table == null)
                return false;
            if (seatIndex != currentSeatIndex)
                return false;
            if (currentPhase != TurnPhase.MustDraw)
                return false;
            if (HasPendingLocalDraw())
                return false;
            if (drawTakenThisTurn)
                return false;

            OkeyPlayerSeat seat = GetSeat(seatIndex);
            if (!CanSeatDraw(seat))
                return false;

            int before = seat.HandCount;
            OkeyTileInstance tile = Table.DrawFromTop();
            if (tile == null)
            {
                DeclareDrawIfDeckEmpty();
                return false;
            }

            ApplyTableVisual(tile);
            seat.AddToHand(tile);

            if (seat.HandCount != before + 1 || seat.HandCount != 15)
            {
                seat.RemoveFromHand(tile);
                Table.ReturnToBottom(tile);
                return false;
            }

            drawTakenThisTurn = true;
            currentPhase = TurnPhase.MustDiscard;
            return true;
        }

        public bool TryTakeTopDiscardForSeat(int seatIndex, OkeyDiscardPile pile)
        {
            SyncTurnStateWithSeat(seatIndex);

            if (IsGameLocked())
                return false;
            if (seatIndex != currentSeatIndex)
                return false;
            if (currentPhase != TurnPhase.MustDraw)
                return false;
            if (pile == null || !pile.HasTile)
                return false;
            if (HasPendingLocalDraw())
                return false;
            if (drawTakenThisTurn)
                return false;

            OkeyPlayerSeat seat = GetSeat(seatIndex);
            if (!CanSeatDraw(seat))
                return false;

            int before = seat.HandCount;
            OkeyTileInstance tile = pile.TakeTopTile();
            if (tile == null)
                return false;

            ApplyTableVisual(tile);
            seat.AddToHand(tile);

            if (seat.HandCount != before + 1 || seat.HandCount != 15)
            {
                seat.RemoveFromHand(tile);
                pile.RestoreTopTile(tile);
                return false;
            }

            drawTakenThisTurn = true;
            currentPhase = TurnPhase.MustDiscard;
            return true;
        }

        public bool TryTakeTopDiscardForLocalPlayer(OkeyDiscardPile pile)
        {
            return TryTakeTopDiscardForSeat(LocalSeatIndex, pile);
        }

        public bool TryDiscardForLocalPlayer(OkeyTileInstance tile)
        {
            return TryDiscardForSeat(LocalSeatIndex, tile);
        }

        public bool TryDiscardForSeat(int seatIndex, OkeyTileInstance tile)
        {
            RefreshSeatCacheIfNeeded();
            SyncTurnStateWithSeat(seatIndex);

            if (IsGameLocked())
            {
                Debug.LogWarning("[OkeyTurnManager] Reject discard: game locked.");
                return false;
            }

            if (seatIndex != currentSeatIndex)
            {
                Debug.LogWarning($"[OkeyTurnManager] Reject discard: seat {seatIndex} is not current seat {currentSeatIndex}.");
                return false;
            }

            OkeyPlayerSeat seat = GetSeat(seatIndex);
            if (seat == null)
            {
                Debug.LogWarning("[OkeyTurnManager] Reject discard: seat is null.");
                return false;
            }

            if (tile == null)
            {
                Debug.LogWarning("[OkeyTurnManager] Reject discard: tile is null.");
                return false;
            }

            bool isDirectPendingDiscard =
                seatIndex == LocalSeatIndex &&
                pendingDrawTile != null &&
                pendingDrawTile == tile &&
                !seat.Contains(tile);

            if (seatIndex == LocalSeatIndex &&
                pendingDrawTile != null &&
                seat.Contains(pendingDrawTile) &&
                seat.HandCount >= 15)
            {
                drawTakenThisTurn = true;
                currentPhase = TurnPhase.MustDiscard;
                ClearPendingDrawState();
            }

            if (currentPhase != TurnPhase.MustDiscard)
            {
                if (seat.HandCount >= 15 || isDirectPendingDiscard)
                {
                    drawTakenThisTurn = true;
                    currentPhase = TurnPhase.MustDiscard;
                }
                else
                {
                    Debug.LogWarning($"[OkeyTurnManager] Reject discard: phase is {currentPhase}, handCount={seat.HandCount}, drawTaken={drawTakenThisTurn}.");
                    return false;
                }
            }

            if (!seat.Contains(tile) && !isDirectPendingDiscard)
            {
                Debug.LogWarning($"[OkeyTurnManager] Reject discard: tile {tile.name} is not in seat {seatIndex} hand.");
                return false;
            }

            if (!isDirectPendingDiscard && seat.HandCount < 15)
            {
                Debug.LogWarning($"[OkeyTurnManager] Reject discard: handCount={seat.HandCount}, expected 15.");
                return false;
            }

            if (!isDirectPendingDiscard)
                seat.RemoveFromHand(tile);

            ApplyTableVisual(tile);

            if (seat.DiscardPile != null)
                seat.DiscardPile.PlaceDiscardedTile(tile);
            else
                Debug.LogWarning($"[OkeyTurnManager] Seat {seatIndex} has no DiscardPile assigned.");

            if (isDirectPendingDiscard)
                ClearPendingDrawState();

            if (WinController != null && WinController.TryDeclareWin(seat))
                return true;

            AdvanceTurn();
            return true;
        }

        public OkeyDiscardPile GetPreviousSeatDiscardPile(int seatIndex)
        {
            int previous = seatIndex - 1;
            if (previous < 0)
                previous = 3;

            OkeyPlayerSeat seat = GetSeat(previous);
            return seat != null ? seat.DiscardPile : null;
        }

        private void AdvanceTurn()
        {
            currentSeatIndex = (currentSeatIndex + 1) % 4;
            ClearPendingDrawState();
            drawTakenThisTurn = false;

            OkeyPlayerSeat seat = GetSeat(currentSeatIndex);
            currentPhase = (seat != null && seat.HandCount >= 15)
                ? TurnPhase.MustDiscard
                : TurnPhase.MustDraw;

            StartTurnForCurrentSeat();
        }

        private void StartTurnForCurrentSeat()
        {
            if (IsGameLocked())
                return;

            OkeyPlayerSeat seat = GetSeat(currentSeatIndex);
            if (seat == null)
                return;

            SyncTurnStateWithSeat(currentSeatIndex);

            if (currentSeatIndex == LocalSeatIndex)
                return;

            OkeyBotController bot = seat.GetComponent<OkeyBotController>();
            if (bot != null)
                bot.StartBotTurn(this);
        }

        private OkeyPlayerSeat GetSeat(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex > 3)
                return null;

            return seatByIndex[seatIndex];
        }

        private void ClearPendingDrawState()
        {
            pendingDrawSource = PendingDrawSource.None;
            pendingDrawTile = null;
            pendingDiscardPile = null;
        }
    }
}