using Dynasty.Legacy.CorrosionCollapse.Board;

namespace Dynasty.Legacy.CorrosionCollapse.Players
{
    [System.Serializable]
    public sealed class PlayerState
    {
        public int playerId;
        public string nickname;
        public BoardNode currentNode;
        public int score;
        public bool finished;
        public bool skipNextTurn;
        public bool hasShortcutPass;
        public bool extraRollAvailable;
        public bool extraRollUsedThisTurn;
        public bool isBot;
        public bool eliminated;

        public bool CanAct => !finished && !eliminated;
    }
}
