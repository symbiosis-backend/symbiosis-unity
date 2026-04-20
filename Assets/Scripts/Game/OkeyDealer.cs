using System.Collections.Generic;
using UnityEngine;

namespace OkeyGame
{
    public sealed class OkeyDealer : MonoBehaviour
    {
        [Header("Links")]
        public OkeyTableModule Table;
        public List<OkeyPlayerSeat> Seats = new List<OkeyPlayerSeat>();
        public OkeyTurnManager TurnManager;
        public OkeyIndicatorView IndicatorView;
        public OkeyDrawPile DrawPileView;

        [Header("Piles")]
        public Transform DrawPileRoot;
        public Transform DiscardPileRoot;
        public Transform CanvasRoot;

        [Header("Rules")]
        [Range(0, 3)] public int StartPlayerIndex = 0;

        [Header("Debug")]
        public bool AutoPrepareOnPlay = true;

        private bool roundPrepared;
        private int lastPrepareFrame = -1;

        private void Start()
        {
            if (AutoPrepareOnPlay)
                PrepareNewRound();
        }

        [ContextMenu("Prepare New Round")]
        public void PrepareNewRound()
        {
            if (lastPrepareFrame == Time.frameCount)
            {
                Debug.LogWarning("[OkeyDealer] PrepareNewRound skipped: already called this frame.");
                return;
            }

            lastPrepareFrame = Time.frameCount;

            Debug.Log("[OkeyDealer] PrepareNewRound CALLED");

            if (Table == null)
            {
                Debug.LogError("[OkeyDealer] Table is NULL.");
                return;
            }

            if (Seats == null || Seats.Count != 4)
            {
                Debug.LogError("[OkeyDealer] Seats must contain exactly 4 players.");
                return;
            }

            for (int i = 0; i < Seats.Count; i++)
            {
                if (Seats[i] == null)
                {
                    Debug.LogError($"[OkeyDealer] Seats[{i}] is NULL.");
                    return;
                }
            }

            roundPrepared = false;

            if (TurnManager != null && TurnManager.WinController != null)
                TurnManager.WinController.ResetRound();

            ClearAllSeats();
            ClearAllDiscards();
            ClearDrawPileRoot();
            ClearIndicatorView();
            CleanupLooseTiles();

            Table.BuildDeck(true);

            if (IndicatorView != null && Table.HasIndicator && Table.IndicatorTile != null)
                IndicatorView.ShowIndicator(Table.IndicatorTile);

            DealOpeningHands();

            MoveRemainingDeckToDrawPileRoot();
            RefreshDrawPileVisualStack();

            if (TurnManager != null)
                TurnManager.InitializeRoundAfterDeal(StartPlayerIndex);

            LogHands();

            roundPrepared = true;

            Debug.Log($"[OkeyDealer] ROUND PREPARED. StartPlayer=P{StartPlayerIndex}, DeckLeft={Table.DeckCount}");
        }

        public void PrepareNewRoundAndStartFlow(OkeyRoundFlow roundFlow)
        {
            Debug.Log("[OkeyDealer] PrepareNewRoundAndStartFlow CALLED");

            if (!roundPrepared)
                PrepareNewRound();

            if (roundFlow != null)
                roundFlow.StartPreparedRound();
        }

        private void DealOpeningHands()
        {
            for (int seatIndex = 0; seatIndex < 4; seatIndex++)
            {
                OkeyPlayerSeat seat = GetSeatByIndex(seatIndex);
                if (seat == null)
                {
                    Debug.LogError($"[OkeyDealer] Seat with SeatIndex={seatIndex} not found.");
                    continue;
                }

                int targetCount = seatIndex == StartPlayerIndex ? 15 : 14;

                if (seat.HandCount > 0)
                {
                    Debug.LogWarning($"[OkeyDealer] Seat {seatIndex} already has {seat.HandCount} tiles before deal. Clearing again.");
                    seat.ClearHand();
                }

                for (int i = 0; i < targetCount; i++)
                {
                    OkeyTileInstance tile = Table.DrawFromTop();
                    if (tile == null)
                    {
                        Debug.LogError($"[OkeyDealer] Deck ended during dealing. seat={seatIndex}, target={targetCount}, dealt={i}");
                        return;
                    }

                    bool showFront = TurnManager != null
                        ? seatIndex == TurnManager.LocalSeatIndex
                        : seatIndex == 0;

                    tile.SetFaceVisible(showFront);
                    seat.AddToHand(tile);
                }
            }
        }

        private OkeyPlayerSeat GetSeatByIndex(int seatIndex)
        {
            for (int i = 0; i < Seats.Count; i++)
            {
                if (Seats[i] != null && Seats[i].SeatIndex == seatIndex)
                    return Seats[i];
            }

            return null;
        }

        private void LogHands()
        {
            for (int i = 0; i < Seats.Count; i++)
            {
                if (Seats[i] == null)
                    continue;

                Debug.Log($"[OkeyDealer] Seat {Seats[i].SeatIndex} hand={Seats[i].HandCount}");
            }
        }

        private void ClearAllSeats()
        {
            for (int i = 0; i < Seats.Count; i++)
            {
                if (Seats[i] != null)
                    Seats[i].ClearHand();
            }
        }

        private void ClearAllDiscards()
        {
            if (DiscardPileRoot == null)
                return;

            for (int i = 0; i < DiscardPileRoot.childCount; i++)
            {
                Transform child = DiscardPileRoot.GetChild(i);
                if (child == null)
                    continue;

                var pile = child.GetComponent<OkeyDiscardPile>();
                if (pile != null)
                    pile.ClearPile();
            }
        }

        private void MoveRemainingDeckToDrawPileRoot()
        {
            if (DrawPileRoot == null || Table == null)
                return;

            var toMove = new List<Transform>();

            for (int i = 0; i < Table.transform.childCount; i++)
            {
                Transform child = Table.transform.GetChild(i);
                if (child == null)
                    continue;

                if (child == Table.IndicatorTile?.transform)
                    continue;

                toMove.Add(child);
            }

            for (int i = 0; i < toMove.Count; i++)
                toMove[i].SetParent(DrawPileRoot, false);
        }

        private void RefreshDrawPileVisualStack()
        {
            if (DrawPileRoot == null)
                return;

            for (int i = 0; i < DrawPileRoot.childCount; i++)
            {
                Transform child = DrawPileRoot.GetChild(i);
                if (child == null)
                    continue;

                child.gameObject.SetActive(true);

                var tile = child.GetComponent<OkeyTileInstance>();
                if (tile != null)
                {
                    tile.ShowBack();
                    tile.RefreshVisuals();
                }

                if (child is RectTransform rt)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.localRotation = Quaternion.identity;
                    rt.localScale = Vector3.one;
                }
                else
                {
                    child.localPosition = Vector3.zero;
                    child.localRotation = Quaternion.identity;
                    child.localScale = Vector3.one;
                }
            }

            if (DrawPileView != null)
                DrawPileView.RefreshView();
        }

        private void ClearDrawPileRoot()
        {
            if (DrawPileRoot == null)
                return;

            for (int i = DrawPileRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = DrawPileRoot.GetChild(i);
                if (child == null)
                    continue;

                child.SetParent(Table != null ? Table.transform : null, false);
                child.gameObject.SetActive(false);
            }
        }

        private void ClearIndicatorView()
        {
            if (IndicatorView != null)
                IndicatorView.ClearIndicator();
        }

        private void CleanupLooseTiles()
        {
            Transform root = CanvasRoot != null ? CanvasRoot : (DrawPileRoot != null ? DrawPileRoot.root : null);
            if (root == null)
                return;

            var allTiles = root.GetComponentsInChildren<OkeyTileInstance>(true);
            var loose = new List<GameObject>();

            for (int i = 0; i < allTiles.Length; i++)
            {
                var tile = allTiles[i];
                if (tile == null)
                    continue;

                bool belongsToSeat = false;
                for (int s = 0; s < Seats.Count; s++)
                {
                    if (Seats[s] != null && Seats[s].ContainsTile(tile))
                    {
                        belongsToSeat = true;
                        break;
                    }
                }

                bool isIndicator = IndicatorView != null && tile.transform.IsChildOf(IndicatorView.transform);
                bool isDrawPileTile = DrawPileRoot != null && tile.transform.IsChildOf(DrawPileRoot);
                bool isTableTile = Table != null && tile.transform.IsChildOf(Table.transform);

                if (!belongsToSeat && !isIndicator && !isDrawPileTile && !isTableTile)
                    loose.Add(tile.gameObject);
            }

            for (int i = 0; i < loose.Count; i++)
                ResetLooseTile(loose[i]);
        }

        private void ResetLooseTile(GameObject go)
        {
            if (go == null)
                return;

            if (Table != null)
                go.transform.SetParent(Table.transform, false);
            else
                go.transform.SetParent(null, false);

            var tile = go.GetComponent<OkeyTileInstance>();
            if (tile != null)
            {
                tile.ShowBack();
                tile.RefreshVisuals();
            }

            go.SetActive(false);
        }
    }
}