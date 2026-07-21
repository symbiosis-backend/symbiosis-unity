using UnityEngine;

namespace Dynasty.Legacy.CorrosionCollapse.Networking
{
    public sealed class LocalServerAuthority : MonoBehaviour, IServerAuthority
    {
        public bool IsServer => true;

        public bool IsLocalPlayerTurn(int playerId)
        {
            return playerId == 0;
        }

        public void BroadcastDiceResult(int playerId, int dice1, int dice2)
        {
            Debug.Log($"[Dice] {dice1} + {dice2}");
        }

        public void BroadcastPlayerNode(int playerId, int nodeId)
        {
            Debug.Log($"[Move] Player {playerId} -> Node {nodeId}");
        }

        public void BroadcastNodeState(int nodeId, string state)
        {
            if (state == "Destroyed")
            {
                Debug.Log($"[Corrosion] Node destroyed {nodeId}");
            }
        }

        public void BroadcastMatchResult(int winnerPlayerId)
        {
            Debug.Log($"[Game] Winner player {winnerPlayerId}");
        }
    }
}
