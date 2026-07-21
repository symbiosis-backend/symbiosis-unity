using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OzGame.Okey
{
    public class OkeyServerSim
    {
        private readonly Dictionary<int, OkeyBot> bots = new Dictionary<int, OkeyBot>();
        private readonly Dictionary<int, double> nextBotTime = new Dictionary<int, double>();
        private readonly TileMgr tileMgr;
        private readonly OkeyRulesConfig config;
        private IOkeyRules rules;

        public OkeyMatch Match { get; private set; }
        public OkeyRoundResult LastResult { get; private set; }

        public OkeyServerSim(TileMgr tileMgr, OkeyRulesConfig config)
        {
            this.tileMgr = tileMgr;
            this.config = config;
            rules = config.mode == OkeyMode.Okey101 ? (IOkeyRules)new Okey101Rules() : new DuzRules();
        }

        public OkeyMatch StartBotMatch(string humanId)
        {
            Match = new OkeyMatch
            {
                matchId = Guid.NewGuid().ToString("N"),
                roomId = "local-bots",
                matchSeed = Environment.TickCount,
                dealerSeat = 3,
                direction = OkeyDirection.Anticlockwise
            };

            Match.players.Add(new OkeyPlayer { playerId = humanId, displayName = "Player", seatIndex = 0, isBot = false, score = config.startingScore });
            for (var i = 1; i < 4; i++)
            {
                Match.players.Add(new OkeyPlayer { playerId = $"bot-{i}", displayName = $"Bot {i}", seatIndex = i, isBot = true, score = config.startingScore });
                bots[i] = new OkeyBot();
            }

            rules.InitRound(Match, config);
            rules.Deal(Match, tileMgr, config);
            return Match;
        }

        public bool Apply(OkeyAction action)
        {
            if (Match == null || action == null) return false;
            action.serverTime = Time.realtimeSinceStartupAsDouble;
            Match.actionLog.Add(action);
            var player = Match.players.FirstOrDefault(p => p.playerId == action.playerId);
            if (player == null) return Reject("unknown_player");
            Match.lastError = "";

            switch (action.actionType)
            {
                case OkeyActionType.DrawStock:
                    return DrawStock(player);
                case OkeyActionType.TakeDiscard:
                    return TakeDiscard(player);
                case OkeyActionType.Discard:
                    return Discard(player, action.tileId);
                case OkeyActionType.DeclareWin:
                    return DeclareWin(player, action.tileId);
                case OkeyActionType.CifteGit:
                    return CifteGit(player);
                default:
                    return Reject("unsupported_action");
            }
        }

        public void TickBots()
        {
            if (Match == null || Match.roundState != OkeyMatchState.Playing) return;
            var player = Match.CurrentPlayer;
            if (player == null || !player.isBot) return;
            var bot = bots[player.seatIndex];
            if (nextBotTime.TryGetValue(player.seatIndex, out var nextTime) && Time.realtimeSinceStartupAsDouble < nextTime) return;
            var move = bot.Decide(Match, player, rules, config);
            if (move.actionType == OkeyActionType.BotMove) return;
            nextBotTime[player.seatIndex] = Time.realtimeSinceStartupAsDouble + Math.Max(0.2f, move.delay);
            Apply(new OkeyAction
            {
                actionId = Guid.NewGuid().ToString("N"),
                playerId = player.playerId,
                actionType = move.actionType,
                tileId = move.tileId,
                payload = move.reason
            });
        }

        public List<OkeyMove> LegalMoves(string playerId)
        {
            var player = Match?.players.FirstOrDefault(p => p.playerId == playerId);
            return player == null ? new List<OkeyMove>() : rules.GetLegalMoves(Match, player);
        }

        private bool DrawStock(OkeyPlayer player)
        {
            if (!rules.CanDraw(Match, player)) return Reject("cannot_draw");
            var tile = tileMgr.Draw(Match);
            if (tile == null) return Reject("empty_stock");
            player.hand.Add(tile);
            Match.turnPhase = TurnPhase.WaitingDiscard;
            return true;
        }

        private bool TakeDiscard(OkeyPlayer player)
        {
            if (!rules.CanTakeDiscard(Match, player)) return Reject("cannot_take_discard");
            var prevSeat = (Match.currentTurnSeat - (int)Match.direction + Match.players.Count) % Match.players.Count;
            var prev = Match.players.First(p => p.seatIndex == prevSeat);
            var tile = prev.discardPile[prev.discardPile.Count - 1];
            prev.discardPile.RemoveAt(prev.discardPile.Count - 1);
            player.hand.Add(tile);
            Match.turnPhase = TurnPhase.WaitingDiscard;
            return true;
        }

        private bool Discard(OkeyPlayer player, int tileId)
        {
            var tile = player.hand.FirstOrDefault(t => t.id == tileId);
            if (!rules.CanDiscard(Match, player, tile)) return Reject("cannot_discard");
            player.hand.Remove(tile);
            player.discardPile.Add(tile);
            Match.currentTurnSeat = (Match.currentTurnSeat + (int)Match.direction + Match.players.Count) % Match.players.Count;
            Match.turnPhase = TurnPhase.WaitingDraw;
            return true;
        }

        private bool DeclareWin(OkeyPlayer player, int finalDiscardTileId)
        {
            if (Match.turnPhase != TurnPhase.WaitingDiscard) return Reject("cannot_declare");

            OkeyTile finalDiscard = null;
            var hand = player.hand.ToList();
            if (finalDiscardTileId >= 0)
            {
                finalDiscard = hand.FirstOrDefault(t => t.id == finalDiscardTileId);
                if (finalDiscard == null) return Reject("missing_final_discard");
                hand.Remove(finalDiscard);
            }
            else if (hand.Count == 15)
            {
                return Reject("final_discard_required");
            }

            if (!rules.ValidateHand(hand, Match.realOkeyTile, config, out var result)) return Reject("invalid_hand");
            if (finalDiscard != null)
            {
                player.hand.Remove(finalDiscard);
                player.discardPile.Add(finalDiscard);
            }
            LastResult = rules.CalculateScore(Match, player, finalDiscard, result.sevenPairs, config);
            return true;
        }

        private bool CifteGit(OkeyPlayer player)
        {
            if (Match.CurrentPlayer != player || Match.roundState != OkeyMatchState.Playing) return Reject("cannot_cifte_git");
            player.cifteGit = true;
            return true;
        }

        private bool Reject(string code)
        {
            if (Match != null) Match.lastError = code;
            return false;
        }
    }
}
