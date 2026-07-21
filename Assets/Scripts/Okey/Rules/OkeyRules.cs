using System.Collections.Generic;

namespace OzGame.Okey
{
    public interface IOkeyRules
    {
        OkeyMode Mode { get; }
        void InitRound(OkeyMatch match, OkeyRulesConfig config);
        void Deal(OkeyMatch match, TileMgr tileMgr, OkeyRulesConfig config);
        bool CanDraw(OkeyMatch match, OkeyPlayer player);
        bool CanDiscard(OkeyMatch match, OkeyPlayer player, OkeyTile tile);
        bool CanTakeDiscard(OkeyMatch match, OkeyPlayer player);
        bool ValidateHand(IReadOnlyList<OkeyTile> hand, OkeyTile realOkey, OkeyRulesConfig config, out OkeyRoundResult result);
        bool ValidateMeld(IReadOnlyList<OkeyTile> tiles, OkeyTile realOkey, OkeyRulesConfig config);
        OkeyRoundResult CalculateScore(OkeyMatch match, OkeyPlayer winner, OkeyTile finalDiscard, bool sevenPairs, OkeyRulesConfig config);
        bool IsRoundFinished(OkeyMatch match);
        List<OkeyMove> GetLegalMoves(OkeyMatch match, OkeyPlayer player);
        List<OkeyMove> GetBotHints(OkeyMatch match, OkeyPlayer player);
    }
}
