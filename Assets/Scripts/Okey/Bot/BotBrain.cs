using System.Linq;

namespace OzGame.Okey
{
    public class BotBrain
    {
        public BotMove Decide(OkeyMatch match, OkeyPlayer bot, IOkeyRules rules, OkeyRulesConfig config, BotMemory memory, BotLevel level)
        {
            if (match.turnPhase == TurnPhase.WaitingDraw)
            {
                if (rules.CanTakeDiscard(match, bot) && ShouldTakeDiscard(match, bot, memory, level))
                    return new BotMove { actionType = OkeyActionType.TakeDiscard, delay = 1.1f, reason = "discard improves hand" };
                return new BotMove { actionType = OkeyActionType.DrawStock, delay = 0.9f, reason = "draw stock" };
            }

            if (match.turnPhase == TurnPhase.WaitingDiscard)
            {
                var winDiscard = FindWinningDiscard(bot, match.realOkeyTile, rules, config);
                if (winDiscard != null)
                    return new BotMove { actionType = OkeyActionType.DeclareWin, tileId = winDiscard.id, delay = 1.2f, reason = "winning hand" };

                var tile = PickDiscard(bot, match.realOkeyTile, memory, level);
                return new BotMove { actionType = OkeyActionType.Discard, tileId = tile?.id ?? -1, delay = 1.0f, reason = "lowest value tile" };
            }

            return new BotMove { actionType = OkeyActionType.BotMove, delay = 1f, reason = "locked" };
        }

        private static bool ShouldTakeDiscard(OkeyMatch match, OkeyPlayer bot, BotMemory memory, BotLevel level)
        {
            var prevSeat = (match.currentTurnSeat - (int)match.direction + match.players.Count) % match.players.Count;
            var prev = match.players.FirstOrDefault(p => p.seatIndex == prevSeat);
            var tile = prev?.discardPile.LastOrDefault();
            if (tile == null) return false;
            return ScoreTile(tile, bot, match.realOkeyTile, memory, level) >= 2;
        }

        private static OkeyTile PickDiscard(OkeyPlayer bot, OkeyTile realOkey, BotMemory memory, BotLevel level)
        {
            return bot.hand.OrderBy(t => ScoreTile(t, bot, realOkey, memory, level)).FirstOrDefault();
        }

        private static OkeyTile FindWinningDiscard(OkeyPlayer bot, OkeyTile realOkey, IOkeyRules rules, OkeyRulesConfig config)
        {
            if (bot.hand.Count == 14 && rules.ValidateHand(bot.hand, realOkey, config, out _)) return null;
            if (bot.hand.Count != 15) return null;

            foreach (var discard in bot.hand.OrderBy(t => ScoreTile(t, bot, realOkey, new BotMemory(), BotLevel.Hard)))
            {
                var hand = bot.hand.Where(t => t.id != discard.id).ToList();
                if (rules.ValidateHand(hand, realOkey, config, out _)) return discard;
            }

            return null;
        }

        private static int ScoreTile(OkeyTile tile, OkeyPlayer player, OkeyTile realOkey, BotMemory memory, BotLevel level)
        {
            if (tile == null) return 0;
            if (tile.isRealOkey) return 100;
            if (tile.type == OkeyTileType.FakeJoker) return 40;

            var score = 0;
            if (player.hand.Any(t => t.id != tile.id && t.color == tile.color && (t.number == tile.number - 1 || t.number == tile.number + 1))) score += 3;
            if (player.hand.Any(t => t.id != tile.id && t.number == tile.number && t.color != tile.color)) score += 3;
            if (player.hand.Any(t => t.id != tile.id && t.number == tile.number && t.color == tile.color)) score += 2;
            if (level != BotLevel.Easy && memory.SeenDiscards.Count(t => t.number == tile.number || t.color == tile.color) > 3) score -= 1;
            return score;
        }
    }
}
