using System.Collections.Generic;

namespace OzGame.Okey
{
    public class DuzRules : IOkeyRules
    {
        public OkeyMode Mode => OkeyMode.DuzOkey;

        public void InitRound(OkeyMatch match, OkeyRulesConfig config)
        {
            match.mode = OkeyMode.DuzOkey;
            match.roundState = OkeyMatchState.Preparing;
            match.turnPhase = TurnPhase.Locked;
            match.winnerSeat = -1;
            match.actionLog.Clear();
            foreach (var player in match.players)
            {
                player.hand.Clear();
                player.discardPile.Clear();
                player.isReady = true;
            }
        }

        public void Deal(OkeyMatch match, TileMgr tileMgr, OkeyRulesConfig config)
        {
            tileMgr.BuildRound(match, config);
            var firstSeat = NextSeat(match.dealerSeat, match);
            match.currentTurnSeat = firstSeat;

            for (var i = 0; i < 14; i++)
            {
                foreach (var player in match.players)
                    player.hand.Add(tileMgr.Draw(match));
            }

            match.players.Find(p => p.seatIndex == firstSeat)?.hand.Add(tileMgr.Draw(match));
            match.roundState = OkeyMatchState.Playing;
            match.turnPhase = TurnPhase.WaitingDiscard;
        }

        public bool CanDraw(OkeyMatch match, OkeyPlayer player)
        {
            return IsTurn(match, player) && match.turnPhase == TurnPhase.WaitingDraw && match.stockPile.Count > 0;
        }

        public bool CanDiscard(OkeyMatch match, OkeyPlayer player, OkeyTile tile)
        {
            return IsTurn(match, player) && match.turnPhase == TurnPhase.WaitingDiscard && tile != null && player.hand.Exists(t => t.id == tile.id);
        }

        public bool CanTakeDiscard(OkeyMatch match, OkeyPlayer player)
        {
            if (!IsTurn(match, player) || match.turnPhase != TurnPhase.WaitingDraw) return false;
            var prev = PreviousSeat(match.currentTurnSeat, match);
            var prevPlayer = match.players.Find(p => p.seatIndex == prev);
            return prevPlayer != null && prevPlayer.discardPile.Count > 0;
        }

        public bool ValidateHand(IReadOnlyList<OkeyTile> hand, OkeyTile realOkey, OkeyRulesConfig config, out OkeyRoundResult result)
        {
            result = new OkeyRoundResult();
            if (hand == null || hand.Count != 14) return false;

            if (config.allowSevenPairs && MeldSolver.IsSevenPairs(hand, realOkey))
            {
                result.finished = true;
                result.sevenPairs = true;
                return true;
            }

            if (!MeldSolver.CanCompleteHand(hand, realOkey, config)) return false;
            result.finished = true;
            return true;
        }

        public bool ValidateMeld(IReadOnlyList<OkeyTile> tiles, OkeyTile realOkey, OkeyRulesConfig config)
        {
            return MeldSolver.IsSet(tiles, realOkey) || MeldSolver.IsRun(tiles, realOkey, config);
        }

        public OkeyRoundResult CalculateScore(OkeyMatch match, OkeyPlayer winner, OkeyTile finalDiscard, bool sevenPairs, OkeyRulesConfig config)
        {
            var result = new OkeyRoundResult { finished = true, winnerSeat = winner.seatIndex, sevenPairs = sevenPairs };
            result.jokerFinish = finalDiscard != null && finalDiscard.isRealOkey;
            var loss = (sevenPairs || result.jokerFinish) ? 4 : 2;

            foreach (var player in match.players)
            {
                if (player.seatIndex == winner.seatIndex)
                {
                    result.scoreDelta[player.seatIndex] = 0;
                    continue;
                }

                player.score -= loss;
                result.scoreDelta[player.seatIndex] = -loss;
            }

            match.winnerSeat = winner.seatIndex;
            match.roundState = OkeyMatchState.RoundEnding;
            match.turnPhase = TurnPhase.Locked;
            return result;
        }

        public bool IsRoundFinished(OkeyMatch match)
        {
            return match.roundState == OkeyMatchState.RoundEnding || match.roundState == OkeyMatchState.MatchEnding;
        }

        public List<OkeyMove> GetLegalMoves(OkeyMatch match, OkeyPlayer player)
        {
            var moves = new List<OkeyMove>();
            if (!IsTurn(match, player)) return moves;
            if (match.turnPhase == TurnPhase.WaitingDraw)
            {
                moves.Add(new OkeyMove { type = OkeyActionType.DrawStock });
                if (CanTakeDiscard(match, player)) moves.Add(new OkeyMove { type = OkeyActionType.TakeDiscard });
            }
            else if (match.turnPhase == TurnPhase.WaitingDiscard)
            {
                foreach (var tile in player.hand)
                    moves.Add(new OkeyMove { type = OkeyActionType.Discard, tileId = tile.id });
                moves.Add(new OkeyMove { type = OkeyActionType.DeclareWin });
            }
            return moves;
        }

        public List<OkeyMove> GetBotHints(OkeyMatch match, OkeyPlayer player)
        {
            return GetLegalMoves(match, player);
        }

        private static bool IsTurn(OkeyMatch match, OkeyPlayer player)
        {
            return player != null && match.currentTurnSeat == player.seatIndex && match.roundState == OkeyMatchState.Playing;
        }

        private static int NextSeat(int seat, OkeyMatch match)
        {
            var count = match.players.Count;
            return (seat + (int)match.direction + count) % count;
        }

        private static int PreviousSeat(int seat, OkeyMatch match)
        {
            var count = match.players.Count;
            return (seat - (int)match.direction + count) % count;
        }
    }
}
