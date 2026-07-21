using System.Collections.Generic;

namespace OzGame.Okey
{
    public class Okey101Rules : IOkeyRules
    {
        public OkeyMode Mode => OkeyMode.Okey101;

        public void InitRound(OkeyMatch match, OkeyRulesConfig config)
        {
            match.mode = OkeyMode.Okey101;
            match.roundState = OkeyMatchState.WaitingPlayers;
            match.lastError = "Okey 101 is scaffolded, not playable in Stage 1.";
        }

        public void Deal(OkeyMatch match, TileMgr tileMgr, OkeyRulesConfig config) { }
        public bool CanDraw(OkeyMatch match, OkeyPlayer player) => false;
        public bool CanDiscard(OkeyMatch match, OkeyPlayer player, OkeyTile tile) => false;
        public bool CanTakeDiscard(OkeyMatch match, OkeyPlayer player) => false;
        public bool ValidateHand(IReadOnlyList<OkeyTile> hand, OkeyTile realOkey, OkeyRulesConfig config, out OkeyRoundResult result) { result = new OkeyRoundResult(); return false; }
        public bool ValidateMeld(IReadOnlyList<OkeyTile> tiles, OkeyTile realOkey, OkeyRulesConfig config) => MeldSolver.IsSet(tiles, realOkey) || MeldSolver.IsRun(tiles, realOkey, config);
        public OkeyRoundResult CalculateScore(OkeyMatch match, OkeyPlayer winner, OkeyTile finalDiscard, bool sevenPairs, OkeyRulesConfig config) => new OkeyRoundResult();
        public bool IsRoundFinished(OkeyMatch match) => false;
        public List<OkeyMove> GetLegalMoves(OkeyMatch match, OkeyPlayer player) => new List<OkeyMove>();
        public List<OkeyMove> GetBotHints(OkeyMatch match, OkeyPlayer player) => new List<OkeyMove>();
    }
}
