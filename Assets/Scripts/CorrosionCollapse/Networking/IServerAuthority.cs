namespace Dynasty.Legacy.CorrosionCollapse.Networking
{
    public interface IServerAuthority
    {
        bool IsServer { get; }
        bool IsLocalPlayerTurn(int playerId);
        void BroadcastDiceResult(int playerId, int dice1, int dice2);
        void BroadcastPlayerNode(int playerId, int nodeId);
        void BroadcastNodeState(int nodeId, string state);
        void BroadcastMatchResult(int winnerPlayerId);
    }
}
