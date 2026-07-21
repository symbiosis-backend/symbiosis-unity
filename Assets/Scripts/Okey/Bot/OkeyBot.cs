namespace OzGame.Okey
{
    public class OkeyBot
    {
        private readonly BotBrain brain = new BotBrain();
        private readonly BotMemory memory = new BotMemory();

        public BotLevel level = BotLevel.Normal;

        public BotMove Decide(OkeyMatch match, OkeyPlayer bot, IOkeyRules rules, OkeyRulesConfig config)
        {
            foreach (var player in match.players)
            {
                if (player.discardPile.Count > 0)
                    memory.RememberDiscard(player.discardPile[player.discardPile.Count - 1]);
            }
            return brain.Decide(match, bot, rules, config, memory, level);
        }
    }
}
